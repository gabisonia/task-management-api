using FluentValidation;
using MediatR;
using MongoDB.Bson;
using TaskService.Application.Abstractions;
using TaskService.Application.Dtos.Projects;
using TaskService.Domain.ProjectManagement;
using TaskService.Shared;

namespace TaskService.Application.Projects.Commands;

public sealed record CreateProjectCommand(string OwnerId, string Name, string? Description)
    : IRequest<Result<ProjectResponse>>;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}

public sealed class CreateProjectCommandHandler(
    IProjectRepository projectRepository,
    IDateTimeProvider dateTimeProvider,
    ICacheService cache)
    : IRequestHandler<CreateProjectCommand, Result<ProjectResponse>>
{
    public async Task<Result<ProjectResponse>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        bool exists = await projectRepository.ExistsByNameAsync(request.OwnerId, request.Name, null, cancellationToken);
        if (exists)
        {
            return Result<ProjectResponse>.Failure(
                new Error("PROJECT_DUPLICATE", $"Project with name '{request.Name}' already exists for this owner."));
        }

        var project = new Project
        {
            Id = ObjectId.GenerateNewId(),
            OwnerId = request.OwnerId,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = dateTimeProvider.UtcNow,
            UpdatedAt = dateTimeProvider.UtcNow,
            IsDeleted = false
        };

        await projectRepository.CreateAsync(project, cancellationToken);

        // Invalidate project lists for this owner
        await cache.RemoveByPatternAsync($"projects:list:{request.OwnerId}:*", cancellationToken);

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
