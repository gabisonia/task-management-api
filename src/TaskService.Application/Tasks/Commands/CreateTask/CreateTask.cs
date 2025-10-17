using MediatR;
using MongoDB.Bson;
using TaskService.Application.Abstractions;
using TaskService.Application.Dtos.Tasks;
using TaskService.Domain.ProjectManagement;
using TaskService.Domain.TaskItemManagement;
using TaskService.Shared;

namespace TaskService.Application.Tasks.Commands.CreateTask;

public sealed record CreateTaskCommand(
    string ProjectId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    Priority Priority,
    string? AssigneeUserId,
    DateTime? DueDate,
    string[] Tags) : IRequest<Result<TaskResponse>>;

public sealed class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Result<TaskResponse>>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateTaskCommandHandler(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<TaskResponse>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        Project? project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
        {
            return Result<TaskResponse>.Failure(
                new Error("PROJECT_NOT_FOUND", $"Project with ID '{request.ProjectId}' not found."));
        }

        var task = new TaskItem
        {
            Id = ObjectId.GenerateNewId(),
            ProjectId = ObjectId.Parse(request.ProjectId),
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            Priority = request.Priority,
            AssigneeUserId = request.AssigneeUserId,
            DueDate = request.DueDate,
            Tags = request.Tags,
            CreatedAt = _dateTimeProvider.UtcNow,
            UpdatedAt = _dateTimeProvider.UtcNow,
            IsDeleted = false
        };

        await _taskRepository.CreateAsync(task, cancellationToken);

        var response = new TaskResponse
        {
            Id = task.Id.ToString(),
            ProjectId = task.ProjectId.ToString(),
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            AssigneeUserId = task.AssigneeUserId,
            DueDate = task.DueDate,
            Tags = task.Tags,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };

        return Result<TaskResponse>.Success(response);
    }
}
