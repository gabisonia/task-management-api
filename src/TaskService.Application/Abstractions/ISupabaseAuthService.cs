using TaskService.Application.Dtos.Auth;
using TaskService.Shared;

namespace TaskService.Application.Abstractions;

public interface ISupabaseAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
