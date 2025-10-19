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
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));
        services.AddSingleton<MongoDbContext>();
        services.AddHostedService<MongoDbIndexInitializer>();

        services.AddScoped<IProjectRepository, MongoProjectRepository>();
        services.AddScoped<ITaskRepository, MongoTaskRepository>();

        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            RedisOptions redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()
                                        ?? new RedisOptions();

            return ConnectionMultiplexer.Connect(redisOptions.ConnectionString);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        services.Configure<SupabaseOptions>(configuration.GetSection(SupabaseOptions.SectionName));
        services.AddHttpClient("Supabase");
        services.AddScoped<ISupabaseAuthService, SupabaseAuthService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserAccessor>();

        var supabaseOptions = configuration.GetSection(SupabaseOptions.SectionName).Get<SupabaseOptions>()
                              ?? new SupabaseOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Audience = supabaseOptions.Audience;

                bool usingInsecureIssuer =
                    supabaseOptions.Issuer.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
                options.RequireHttpsMetadata = !usingInsecureIssuer;

                options.MapInboundClaims = false;

                var tvp = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = supabaseOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = supabaseOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
                    RoleClaimType = "role"
                };

                if (!string.IsNullOrWhiteSpace(supabaseOptions.JwtSecret))
                {
                    var keyBytes = Encoding.UTF8.GetBytes(supabaseOptions.JwtSecret);
                    tvp.IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
                    tvp.ValidAlgorithms = new[] { Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256 };
                }
                else if (!string.IsNullOrWhiteSpace(supabaseOptions.JwksUrl))
                {
                    tvp.IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                    {
                        using var http = new HttpClient();
                        var jwksJson = http.GetStringAsync(supabaseOptions.JwksUrl).GetAwaiter().GetResult();
                        var jwks = new Microsoft.IdentityModel.Tokens.JsonWebKeySet(jwksJson);
                        return jwks.Keys
                            .Where(k => string.IsNullOrEmpty(kid) || k.Kid == kid)
                            .Select(JsonWebKeyToSecurityKey)
                            .Where(static key => key is not null)
                            .Cast<Microsoft.IdentityModel.Tokens.SecurityKey>()
                            .ToList();
                    };
                }

                options.TokenValidationParameters = tvp;

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

    private static byte[] DecodeBase64Url(string base64Url)
    {
        string padded = base64Url.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }
}
