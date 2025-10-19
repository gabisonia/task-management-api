using FluentValidation;
using MediatR;
using TaskService.Application.Abstractions;
using TaskService.Application.Dtos.Auth;
using TaskService.Shared;

namespace TaskService.Application.Auth.Commands;

public sealed record LoginUserCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;

public sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginUserCommandHandler(ISupabaseAuthService auth)
    : IRequestHandler<LoginUserCommand, Result<AuthResponse>>
{
    public Task<Result<AuthResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        return auth.LoginAsync(new LoginRequest { Email = request.Email, Password = request.Password },
            cancellationToken);
    }
}
