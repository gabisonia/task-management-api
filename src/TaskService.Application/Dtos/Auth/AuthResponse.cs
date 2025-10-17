namespace TaskService.Application.Dtos.Auth;

public sealed class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public UserInfoResponse User { get; set; } = new();
}

public sealed class UserInfoResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string[] Roles { get; set; } = [];
    public bool EmailVerified { get; set; }
}

