using FluentValidation;
using MediatR;
using TaskService.Application.Abstractions;
using TaskService.Application.Dtos.Auth;
using TaskService.Shared;

namespace TaskService.Application.Auth.Commands;

public sealed record RegisterUserCommand(string Email, string Password, string? DisplayName)
    : IRequest<Result<AuthResponse>>;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public sealed class RegisterUserCommandHandler(ISupabaseAuthService auth)
    : IRequestHandler<RegisterUserCommand, Result<AuthResponse>>
{
    public Task<Result<AuthResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        return auth.RegisterAsync(
            new RegisterRequest
            {
                Email = request.Email, Password = request.Password, DisplayName = request.DisplayName
            }, cancellationToken);
    }
}
