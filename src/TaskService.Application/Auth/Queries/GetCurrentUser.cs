using MediatR;
using TaskService.Application.Abstractions;
using TaskService.Application.Dtos.Auth;
using TaskService.Shared;

namespace TaskService.Application.Auth.Queries;

public sealed record GetCurrentUserQuery() : IRequest<Result<UserInfoResponse>>;

public sealed class GetCurrentUserQueryHandler(ICurrentUser currentUser)
    : IRequestHandler<GetCurrentUserQuery, Result<UserInfoResponse>>
{
    public Task<Result<UserInfoResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.Id))
        {
            return Task.FromResult(Result<UserInfoResponse>.Failure(
                new Error("AUTH_INVALID_TOKEN", "User is not authenticated")));
        }

        var user = new UserInfoResponse
        {
            Id = currentUser.Id,
            Email = currentUser.Email ?? string.Empty,
            DisplayName = currentUser.DisplayName,
            EmailVerified = currentUser.EmailVerified,
            Roles = currentUser.Roles
        };

        return Task.FromResult(Result<UserInfoResponse>.Success(user));
    }
}

