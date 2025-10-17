using FluentValidation;
using MediatR;
using MongoDB.Bson;
using TaskService.Application.Abstractions;
using TaskService.Application.Dtos.Projects;
using TaskService.Domain.ProjectManagement;
using TaskService.Shared;

namespace TaskService.Application.Projects.Commands.CreateProject;

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

public sealed class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Result<ProjectResponse>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateProjectCommandHandler(IProjectRepository projectRepository, IDateTimeProvider dateTimeProvider)
    {
        _projectRepository = projectRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<ProjectResponse>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        bool exists = await _projectRepository.ExistsByNameAsync(request.OwnerId, request.Name, null, cancellationToken);
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
            CreatedAt = _dateTimeProvider.UtcNow,
            UpdatedAt = _dateTimeProvider.UtcNow,
            IsDeleted = false
        };

        await _projectRepository.CreateAsync(project, cancellationToken);

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
