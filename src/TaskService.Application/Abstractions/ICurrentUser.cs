namespace TaskService.Application.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string Id { get; }
    string? Email { get; }
    string? DisplayName { get; }
    bool EmailVerified { get; }
    string[] Roles { get; }
}

