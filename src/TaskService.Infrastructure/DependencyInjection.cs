using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TaskService.Application.Abstractions;
using TaskService.Domain.ProjectManagement;
using TaskService.Domain.TaskItemManagement;
using TaskService.Infrastructure.Authentication;
using TaskService.Infrastructure.Common;
using TaskService.Infrastructure.Persistence.MongoDB;
using TaskService.Infrastructure.Persistence.MongoDB.Repositories;
using TaskService.Infrastructure.Persistence.Redis;
using TaskService.Shared;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TaskService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // DateTime provider
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // MongoDB
        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));
        services.AddSingleton<MongoDbContext>();
        services.AddHostedService<MongoDbIndexInitializer>();

        // MongoDB Repositories
        services.AddScoped<IProjectRepository, MongoProjectRepository>();
        services.AddScoped<ITaskRepository, MongoTaskRepository>();

        // Redis
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            RedisOptions redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()
                                        ?? new RedisOptions();

            return ConnectionMultiplexer.Connect(redisOptions.ConnectionString);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        // Supabase Authentication
        services.Configure<SupabaseOptions>(configuration.GetSection(SupabaseOptions.SectionName));
        services.AddHttpClient("Supabase");
        services.AddScoped<ISupabaseAuthService, SupabaseAuthService>();

        // JWT Authentication
        var supabaseOptions = configuration.GetSection(SupabaseOptions.SectionName).Get<SupabaseOptions>()
                              ?? new SupabaseOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Audience = supabaseOptions.Audience;

                bool usingInsecureIssuer =
                    supabaseOptions.Issuer.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
                if (usingInsecureIssuer)
                {
                    options.RequireHttpsMetadata = false;
                }
                else
                {
                    options.Authority = supabaseOptions.Issuer;
                }

                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = supabaseOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = supabaseOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };

                if (!usingInsecureIssuer)
                {
                    options.TokenValidationParameters.IssuerSigningKeyResolver =
                        (token, securityToken, kid, parameters) =>
                        {
                            using var http = new HttpClient();
                            var jwksJson = http.GetStringAsync(supabaseOptions.JwksUrl).GetAwaiter().GetResult();
                            var jwks = new Microsoft.IdentityModel.Tokens.JsonWebKeySet(jwksJson);

                            var keys = jwks.Keys
                                .Where(k => string.IsNullOrEmpty(kid) || k.Kid == kid)
                                .Select(JsonWebKeyToSecurityKey)
                                .Where(static key => key is not null)
                                .Cast<Microsoft.IdentityModel.Tokens.SecurityKey>()
                                .ToList();

                            if (keys.Count == 0 && !string.IsNullOrWhiteSpace(supabaseOptions.ServiceKey) &&
                                TryCreateSymmetricKeyFromSecret(supabaseOptions.ServiceKey, kid, out var fallbackKey))
                            {
                                keys.Add(fallbackKey);
                            }

                            return keys;
                        };
                }
                else if (!string.IsNullOrWhiteSpace(supabaseOptions.ServiceKey))
                {
                    Console.WriteLine("[Auth] Using local Supabase stub signing key");
                    options.TokenValidationParameters.IssuerSigningKeyResolver =
                        (token, securityToken, kid, parameters) =>
                        {
                            if (TryCreateSymmetricKeyFromSecret(supabaseOptions.ServiceKey, kid, out var key))
                            {
                                return new[] { key };
                            }

                            return Array.Empty<Microsoft.IdentityModel.Tokens.SecurityKey>();
                        };
                }

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer");
                        logger.LogWarning(context.Exception, "JWT authentication failed");
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    private static Microsoft.IdentityModel.Tokens.SecurityKey? JsonWebKeyToSecurityKey(
        Microsoft.IdentityModel.Tokens.JsonWebKey jwk)
    {
        if (string.Equals(jwk.Kty, "oct", StringComparison.OrdinalIgnoreCase) && jwk.K is not null)
        {
            var keyBytes = DecodeBase64Url(jwk.K);
            return new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes) { KeyId = jwk.Kid };
        }

        if (string.Equals(jwk.Kty, "RSA", StringComparison.OrdinalIgnoreCase) && jwk.N is not null && jwk.E is not null)
        {
            return jwk;
        }

        return null;
    }

    private static bool TryCreateSymmetricKeyFromSecret(string secret, string? kid,
        out Microsoft.IdentityModel.Tokens.SecurityKey securityKey)
    {
        try
        {
            var bytes = DecodeBase64Url(secret);
            return CreateKey(bytes, kid, out securityKey);
        }
        catch (FormatException)
        {
            // Not base64 – will treat as plain text below.
        }

        var fallback = Encoding.UTF8.GetBytes(secret);
        var result = CreateKey(fallback, kid, out securityKey);
        Console.WriteLine($"[Auth] Derived symmetric key with kid '{securityKey.KeyId}' and secret '{secret}'");
        return result;
    }

    private static byte[] DecodeBase64Url(string base64Url)
    {
        string padded = base64Url.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }

    private static bool CreateKey(byte[] keyBytes, string? kid,
        out Microsoft.IdentityModel.Tokens.SecurityKey securityKey)
    {
        securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes)
        {
            KeyId = kid ?? ComputeKeyId(keyBytes)
        };
        return true;
    }

    private static string ComputeKeyId(byte[] keyBytes)
    {
        var hash = SHA256.HashData(keyBytes);
        return Convert.ToBase64String(hash).TrimEnd('=');
    }
}
