using FluentValidation;
using MediatR;
using TaskService.Application.Abstractions;
using TaskService.Domain.ProjectManagement;
using TaskService.Shared;

namespace TaskService.Application.Projects.Commands;

public sealed record DeleteProjectCommand(string Id) : IRequest<Result>
{
    public string? IfMatch { get; init; }
}

public sealed class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
{
    public DeleteProjectCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeleteProjectCommandHandler(IProjectRepository projectRepository, ICacheService cache)
    : IRequestHandler<DeleteProjectCommand, Result>
{
    public async Task<Result> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        Project? project = await projectRepository.GetByIdAsync(request.Id, cancellationToken);
        if (project == null)
        {
            return Result.Failure(new Error("PROJECT_NOT_FOUND", $"Project with ID '{request.Id}' not found."));
        }

        // Concurrency check via ETag (If-Match)
        var currentEtag = ETagGenerator.From(project.Id.ToString(), project.UpdatedAt);
        if (!string.IsNullOrWhiteSpace(request.IfMatch) && !string.Equals(request.IfMatch, currentEtag, StringComparison.Ordinal))
        {
            return Result.Failure(
                new Error("PRECONDITION_FAILED", "The resource has been modified. ETag mismatch."));
        }

        await projectRepository.DeleteAsync(request.Id, cancellationToken);

        // Invalidate related caches
        await cache.RemoveAsync($"projects:{project.Id}", cancellationToken);
        await cache.RemoveByPatternAsync($"projects:list:{project.OwnerId}:*", cancellationToken);
        await cache.RemoveByPatternAsync($"tasks:project:{project.Id}:*", cancellationToken);
        return Result.Success();
    }
}
