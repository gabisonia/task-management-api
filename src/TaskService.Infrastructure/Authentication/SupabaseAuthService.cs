using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskService.Application.Abstractions;
using TaskService.Application.Dtos.Auth;
using TaskService.Shared;

namespace TaskService.Infrastructure.Authentication;

public sealed class SupabaseAuthService : ISupabaseAuthService
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseOptions _options;
    private readonly ILogger<SupabaseAuthService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SupabaseAuthService(
        HttpClient httpClient,
        IOptions<SupabaseOptions> options,
        ILogger<SupabaseAuthService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        _httpClient.BaseAddress = new Uri(_options.Url);
        var apiKey = string.IsNullOrWhiteSpace(_options.ApiKey) ? _options.ServiceKey : _options.ApiKey;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("apikey", apiKey);
            // Some endpoints expect Authorization: Bearer <anon_key>
            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }
        }
        if (_options.SkipEmailConfirmation && !_httpClient.DefaultRequestHeaders.Contains("Prefer"))
        {
            _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
        }
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                email = request.Email,
                password = request.Password,
                data = request.DisplayName != null ? new { display_name = request.DisplayName } : null
            };

            var signupEndpoint = _options.SkipEmailConfirmation
                ? "/auth/v1/signup?skip_email_confirmation=true"
                : "/auth/v1/signup";

            var response = await _httpClient.PostAsJsonAsync(
                signupEndpoint,
                payload,
                _jsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Supabase registration failed: {StatusCode}", response.StatusCode);

                return Result<AuthResponse>.Failure(
                    new Error("AUTH_REGISTRATION_FAILED", $"Registration failed: {response.ReasonPhrase}"));
            }

            var supabaseResponse = await response.Content
                .ReadFromJsonAsync<SupabaseAuthResponse>(_jsonOptions, cancellationToken);

            // Ignore session return; clients will log in after registration to get a token.
            var userEmail = supabaseResponse?.User?.Email ?? request.Email;
            var displayName = supabaseResponse?.User?.UserMetadata?.DisplayName ?? request.DisplayName;
            var resultPayload = new AuthResponse
            {
                AccessToken = string.Empty,
                RefreshToken = null,
                ExpiresAt = DateTime.UtcNow,
                User = new UserInfoResponse
                {
                    Id = supabaseResponse?.User?.Id ?? string.Empty,
                    Email = userEmail,
                    DisplayName = displayName,
                    EmailVerified = supabaseResponse?.User?.EmailConfirmedAt != null,
                    Roles = supabaseResponse?.User?.Role != null ? new[] { supabaseResponse.User.Role } : Array.Empty<string>()
                }
            };

            return Result<AuthResponse>.Success(resultPayload);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during Supabase registration");
            return Result<AuthResponse>.Failure(
                new Error("AUTH_SERVICE_UNAVAILABLE", "Authentication service unavailable"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration");
            return Result<AuthResponse>.Failure(
                new Error("AUTH_UNEXPECTED_ERROR", "An unexpected error occurred during registration"));
        }
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { email = request.Email, password = request.Password };

            var response = await _httpClient.PostAsJsonAsync(
                "/auth/v1/token?grant_type=password",
                payload,
                _jsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Supabase login failed: {StatusCode}", response.StatusCode);

                return Result<AuthResponse>.Failure(
                    new Error("AUTH_UNAUTHORIZED", "Invalid email or password"));
            }

            var supabaseResponse = await response.Content
                .ReadFromJsonAsync<SupabaseAuthResponse>(_jsonOptions, cancellationToken);

            if (supabaseResponse == null)
            {
                return Result<AuthResponse>.Failure(
                    new Error("AUTH_LOGIN_FAILED", "Login succeeded but no response returned"));
            }

            var authResponse = MapToAuthResponse(supabaseResponse);
            return Result<AuthResponse>.Success(authResponse);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during Supabase login");
            return Result<AuthResponse>.Failure(
                new Error("AUTH_SERVICE_UNAVAILABLE", "Authentication service unavailable"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            return Result<AuthResponse>.Failure(
                new Error("AUTH_UNEXPECTED_ERROR", "An unexpected error occurred during login"));
        }
    }

    private static AuthResponse MapToAuthResponse(SupabaseAuthResponse supabaseResponse)
    {
        // Supabase returns expires_in as a relative duration (seconds)
        var expiresAt = DateTime.UtcNow;
        try
        {
            if (supabaseResponse.ExpiresIn > 0)
            {
                expiresAt = DateTime.UtcNow.AddSeconds(supabaseResponse.ExpiresIn);
            }
        }
        catch
        {
            // ignore and keep default
        }

        return new AuthResponse
        {
            AccessToken = supabaseResponse.AccessToken ?? string.Empty,
            RefreshToken = supabaseResponse.RefreshToken,
            ExpiresAt = expiresAt,
            User = new UserInfoResponse
            {
                Id = supabaseResponse.User?.Id ?? string.Empty,
                Email = supabaseResponse.User?.Email ?? string.Empty,
                DisplayName = supabaseResponse.User?.UserMetadata?.DisplayName,
                EmailVerified = supabaseResponse.User?.EmailConfirmedAt != null,
                Roles = supabaseResponse.User?.Role != null
                    ? new[] { supabaseResponse.User.Role }
                    : Array.Empty<string>()
            }
        };
    }

    private sealed class SupabaseAuthResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("session")]
        public SupabaseSession? Session { get; set; }

        [JsonPropertyName("user")]
        public SupabaseUser? User { get; set; }
    }

    private sealed class SupabaseSession
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public long ExpiresIn { get; set; }
    }

    private sealed class SupabaseUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("email_confirmed_at")]
        public DateTime? EmailConfirmedAt { get; set; }

        [JsonPropertyName("user_metadata")]
        public SupabaseUserMetadata? UserMetadata { get; set; }
    }

    private sealed class SupabaseUserMetadata
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }
}
