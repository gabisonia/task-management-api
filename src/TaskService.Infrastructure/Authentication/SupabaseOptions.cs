namespace TaskService.Infrastructure.Authentication;

public sealed class SupabaseOptions
{
    public const string SectionName = "Supabase";
    public string Url { get; set; } = string.Empty;
    public string JwksUrl { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = "authenticated";
    public string ServiceKey { get; set; } = string.Empty;
    public bool SkipEmailConfirmation { get; set; }
}
