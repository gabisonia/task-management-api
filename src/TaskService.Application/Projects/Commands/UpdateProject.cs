using FluentValidation;
using MediatR;
using TaskService.Application.Abstractions;
using TaskService.Application.Dtos.Projects;
using TaskService.Domain.ProjectManagement;
using TaskService.Shared;

namespace TaskService.Application.Projects.Commands;

public sealed record UpdateProjectCommand(string Id, string Name, string? Description)
    : IRequest<Result<ProjectResponse>>
{
    public string? IfMatch { get; init; }
}

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}

public sealed class UpdateProjectCommandHandler(
    IProjectRepository projectRepository,
    IDateTimeProvider dateTimeProvider,
    ICacheService cache)
    : IRequestHandler<UpdateProjectCommand, Result<ProjectResponse>>
{
    public async Task<Result<ProjectResponse>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        Project? project = await projectRepository.GetByIdAsync(request.Id, cancellationToken);
        if (project == null)
        {
            return Result<ProjectResponse>.Failure(
                new Error("PROJECT_NOT_FOUND", $"Project with ID '{request.Id}' not found."));
        }

        // Concurrency check via ETag (If-Match)
        var currentEtag = ETagGenerator.From(project.Id.ToString(), project.UpdatedAt);
        if (!string.IsNullOrWhiteSpace(request.IfMatch) &&
            !string.Equals(request.IfMatch, currentEtag, StringComparison.Ordinal))
        {
            return Result<ProjectResponse>.Failure(
                new Error("PRECONDITION_FAILED", "The resource has been modified. ETag mismatch."));
        }

        bool exists =
            await projectRepository.ExistsByNameAsync(project.OwnerId, request.Name, request.Id, cancellationToken);
        if (exists)
        {
            return Result<ProjectResponse>.Failure(
                new Error("PROJECT_DUPLICATE", $"Project with name '{request.Name}' already exists for this owner."));
        }

        project.Name = request.Name;
        project.Description = request.Description;
        project.UpdatedAt = dateTimeProvider.UtcNow;

        await projectRepository.UpdateAsync(project, cancellationToken);

        // Invalidate caches related to this project
        await cache.RemoveAsync($"projects:{project.Id}", cancellationToken);
        await cache.RemoveByPatternAsync($"projects:list:{project.OwnerId}:*", cancellationToken);

        var response = new ProjectResponse
        {
            Id = project.Id.ToString(),
            OwnerId = project.OwnerId,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            ETag = ETagGenerator.From(project.Id.ToString(), project.UpdatedAt)
        };

        return Result<ProjectResponse>.Success(response);
    }
}
