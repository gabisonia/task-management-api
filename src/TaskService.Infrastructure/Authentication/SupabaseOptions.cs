namespace TaskService.Infrastructure.Authentication;

public sealed class SupabaseOptions
{
    public const string SectionName = "Supabase";
    public string Url { get; set; } = string.Empty;
    public string JwksUrl { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = "authenticated";
    // Public anon key for client API calls to /auth/v1 endpoints
    public string ApiKey { get; set; } = string.Empty;
    public string ServiceKey { get; set; } = string.Empty;
    public bool SkipEmailConfirmation { get; set; }
    // For HS256 verification; set to your Supabase project's JWT secret
    public string JwtSecret { get; set; } = string.Empty;
}
