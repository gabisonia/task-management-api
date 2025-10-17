using System.Net.Http.Json;
using System.Text.Json;
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
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        _httpClient.BaseAddress = new Uri(_options.Url);
        _httpClient.DefaultRequestHeaders.Add("apikey", _options.ServiceKey);
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

            if (supabaseResponse?.Session == null)
            {
                return Result<AuthResponse>.Failure(
                    new Error("AUTH_REGISTRATION_FAILED", "Registration succeeded but no session returned"));
            }

            var authResponse = MapToAuthResponse(supabaseResponse);
            return Result<AuthResponse>.Success(authResponse);
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
                    new Error("AUTH_LOGIN_FAILED", "Invalid email or password"));
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
        return new AuthResponse
        {
            AccessToken = supabaseResponse.AccessToken ?? string.Empty,
            RefreshToken = supabaseResponse.RefreshToken,
            ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(supabaseResponse.ExpiresIn).UtcDateTime,
            User = new UserInfoResponse
            {
                Id = supabaseResponse.User?.Id ?? string.Empty,
                Email = supabaseResponse.User?.Email ?? string.Empty,
                DisplayName = supabaseResponse.User?.UserMetadata?.DisplayName,
                EmailVerified = supabaseResponse.User?.EmailConfirmedAt != null,
                Roles = supabaseResponse.User?.Role != null
                    ? [supabaseResponse.User.Role]
                    : []
            }
        };
    }

    private sealed class SupabaseAuthResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public long ExpiresIn { get; set; }
        public string? TokenType { get; set; }
        public SupabaseSession? Session { get; set; }
        public SupabaseUser? User { get; set; }
    }

    private sealed class SupabaseSession
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public long ExpiresIn { get; set; }
    }

    private sealed class SupabaseUser
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public DateTime? EmailConfirmedAt { get; set; }
        public SupabaseUserMetadata? UserMetadata { get; set; }
    }

    private sealed class SupabaseUserMetadata
    {
        public string? DisplayName { get; set; }
    }
}
