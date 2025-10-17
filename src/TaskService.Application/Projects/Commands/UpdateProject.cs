using FluentValidation;
using MediatR;
using TaskService.Application.Dtos.Projects;
using TaskService.Domain.ProjectManagement;
using TaskService.Shared;

namespace TaskService.Application.Projects.Commands;

public sealed record UpdateProjectCommand(string Id, string Name, string? Description)
    : IRequest<Result<ProjectResponse>>;

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
    IDateTimeProvider dateTimeProvider)
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

        var response = new ProjectResponse
        {
            Id = project.Id.ToString(),
            OwnerId = project.OwnerId,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };

        return Result<ProjectResponse>.Success(response);
    }
}
