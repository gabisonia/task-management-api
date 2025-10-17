using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new JwtSettings
    {
        Secret = configuration["SUPABASE_STUB_JWT_SECRET"] ?? "super-secret-jwt",
        Issuer = configuration["SUPABASE_STUB_JWT_ISSUER"] ?? "http://supabase-auth:9999",
        Audience = configuration["SUPABASE_STUB_JWT_AUDIENCE"] ?? "authenticated",
        ExpiresInMinutes = int.TryParse(configuration["SUPABASE_STUB_JWT_EXP_MINUTES"], out var minutes) ? minutes : 60
    };
});
builder.Services.AddSingleton<JwtService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

var port = builder.Configuration["SUPABASE_STUB_PORT"] ?? "9999";
app.Urls.Add($"http://0.0.0.0:{port}");

app.MapGet("/.well-known/jwks.json", (JwtService jwt) => Results.Json(jwt.GetJwks()));
app.MapGet("/auth/v1/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/auth/v1/signup", async (HttpRequest request, UserStore store, JwtService jwt) =>
{
    var payload = await request.ReadFromJsonAsync<SignupRequest>();
    if (payload is null || string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.Password))
    {
        return Results.BadRequest(new { error = "invalid_request", error_description = "Email and password are required." });
    }

    var displayName = payload.Data?.DisplayName;
    var result = store.TryCreateUser(payload.Email, payload.Password, displayName, out var user, out var error);
    if (!result)
    {
        return Results.BadRequest(new { error = "user_exists", error_description = error });
    }

    var token = jwt.CreateToken(user);
    var refreshToken = jwt.CreateRefreshToken();

    var response = SupabaseAuthResponse.Create(user, token, refreshToken, jwt.ExpiresAtUnix);
    return Results.Json(response);
});

app.MapPost("/auth/v1/token", async (HttpRequest request, UserStore store, JwtService jwt) =>
{
    var grantType = request.Query["grant_type"].ToString();
    if (!string.Equals(grantType, "password", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "unsupported_grant_type" });
    }

    var payload = await request.ReadFromJsonAsync<LoginRequest>();
    if (payload is null)
    {
        return Results.BadRequest(new { error = "invalid_request" });
    }

    if (!store.TryValidateUser(payload.Email, payload.Password, out var user))
    {
        return Results.BadRequest(new { error = "invalid_grant" });
    }

    var token = jwt.CreateToken(user);
    var refreshToken = jwt.CreateRefreshToken();
    var response = SupabaseAuthResponse.Create(user, token, refreshToken, jwt.ExpiresAtUnix);
    return Results.Json(response);
});

app.Run();

internal sealed record SignupRequest(string Email, string Password, SignupData? Data);
internal sealed record SignupData(string? DisplayName);
internal sealed record LoginRequest(string Email, string Password);

internal sealed class UserStore
{
    private readonly ConcurrentDictionary<string, UserRecord> _users = new(StringComparer.OrdinalIgnoreCase);

    public bool TryCreateUser(string email, string password, string? displayName, out UserRecord user, out string? error)
    {
        var newUser = new UserRecord(Guid.NewGuid().ToString("N"), email.Trim().ToLowerInvariant(), password, displayName);
        if (!_users.TryAdd(newUser.Email, newUser))
        {
            error = "User already exists.";
            user = default!;
            return false;
        }

        error = null;
        user = newUser;
        return true;
    }

    public bool TryValidateUser(string email, string password, out UserRecord user)
    {
        if (_users.TryGetValue(email.Trim().ToLowerInvariant(), out var existing) && existing.Password == password)
        {
            user = existing;
            return true;
        }

        user = default!;
        return false;
    }
}

internal sealed record UserRecord(string Id, string Email, string Password, string? DisplayName);

internal sealed class JwtSettings
{
    public string Secret { get; init; } = "super-secret-jwt";
    public string Issuer { get; init; } = "http://supabase-auth:9999";
    public string Audience { get; init; } = "authenticated";
    public int ExpiresInMinutes { get; init; } = 60;
}

internal sealed class JwtService
{
    private readonly JwtSettings _settings;
    private readonly byte[] _secretBytes;
    private readonly string _keyId;

    public JwtService(JwtSettings settings)
    {
        _settings = settings;
        _secretBytes = Encoding.UTF8.GetBytes(settings.Secret);
        _keyId = Convert.ToBase64String(SHA256.HashData(_secretBytes)).Replace("=", string.Empty);
    }

    public DateTimeOffset ExpiresAt => DateTimeOffset.UtcNow.AddMinutes(_settings.ExpiresInMinutes);

    public long ExpiresAtUnix => ExpiresAt.ToUnixTimeSeconds();

    public string CreateToken(UserRecord user)
    {
        var header = new { alg = "HS256", typ = "JWT", kid = _keyId };
        var now = DateTimeOffset.UtcNow;
        var payload = new
        {
            exp = ExpiresAtUnix,
            iat = now.ToUnixTimeSeconds(),
            aud = _settings.Audience,
            iss = _settings.Issuer,
            sub = user.Id,
            email = user.Email,
            role = "authenticated",
            email_confirmed_at = now.UtcDateTime,
            user_metadata = new { display_name = user.DisplayName }
        };

        var headerJson = JsonSerializer.Serialize(header);
        var payloadJson = JsonSerializer.Serialize(payload);

        var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signature = ComputeSignature($"{headerBase64}.{payloadBase64}");
        return $"{headerBase64}.{payloadBase64}.{signature}";
    }

    public string CreateRefreshToken()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        using var hmac = new HMACSHA256(_secretBytes);
        var hash = hmac.ComputeHash(buffer.ToArray());
        return Base64UrlEncode(hash);
    }

    public object GetJwks()
    {
        return new
        {
            keys = new[]
            {
                new
                {
                    kty = "oct",
                    k = Base64UrlEncode(_secretBytes),
                    alg = "HS256",
                    use = "sig",
                    kid = _keyId
                }
            }
        };
    }

    public string ComputeSignature(string data)
    {
        using var hmac = new HMACSHA256(_secretBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

internal sealed record SupabaseAuthResponse(
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    string RefreshToken,
    SupabaseSession Session,
    SupabaseUser User)
{
    public static SupabaseAuthResponse Create(UserRecord user, string accessToken, string refreshToken, long expiresAt)
    {
        var supabaseUser = new SupabaseUser
        (
            user.Id,
            user.Email,
            "authenticated",
            DateTime.UtcNow,
            new SupabaseUserMetadata(user.DisplayName)
        );

        var session = new SupabaseSession(accessToken, refreshToken, expiresAt, "bearer", supabaseUser);
        return new SupabaseAuthResponse(accessToken, "bearer", expiresAt, refreshToken, session, supabaseUser);
    }
}

internal sealed record SupabaseSession(string AccessToken, string RefreshToken, long ExpiresIn, string TokenType, SupabaseUser User);

internal sealed record SupabaseUser(string Id, string Email, string Role, DateTime EmailConfirmedAt, SupabaseUserMetadata UserMetadata);

internal sealed record SupabaseUserMetadata(string? DisplayName);
