using MediatR;
using TaskService.Application.Abstractions;
using TaskService.Application.Dtos.Tasks;
using TaskService.Domain.TaskItemManagement;
using TaskService.Shared;

namespace TaskService.Application.Tasks.Commands.UpdateTask;

public sealed record UpdateTaskCommand(
    string Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    Priority Priority,
    DateTime? DueDate,
    string[] Tags) : IRequest<Result<TaskResponse>>
{
    public string? IfMatch { get; init; }
}

public sealed class UpdateTaskCommandHandler(ITaskRepository taskRepository, IDateTimeProvider dateTimeProvider, ICacheService cache)
    : IRequestHandler<UpdateTaskCommand, Result<TaskResponse>>
{
    public async Task<Result<TaskResponse>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        TaskItem? task = await taskRepository.GetByIdAsync(request.Id, cancellationToken);
        if (task == null)
        {
            return Result<TaskResponse>.Failure(new Error("TASK_NOT_FOUND", $"Task with ID '{request.Id}' not found."));
        }

        // Concurrency check via ETag (If-Match)
        var currentEtag = ETagGenerator.From(task.Id.ToString(), task.UpdatedAt);
        if (!string.IsNullOrWhiteSpace(request.IfMatch) && !string.Equals(request.IfMatch, currentEtag, StringComparison.Ordinal))
        {
            return Result<TaskResponse>.Failure(
                new Error("PRECONDITION_FAILED", "The resource has been modified. ETag mismatch."));
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.Tags = request.Tags;
        task.UpdatedAt = dateTimeProvider.UtcNow;

        await taskRepository.UpdateAsync(task, cancellationToken);

        // Invalidate caches for this task and related lists
        await cache.RemoveAsync($"tasks:{task.Id}", cancellationToken);
        await cache.RemoveByPatternAsync($"tasks:project:{task.ProjectId}:*", cancellationToken);

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
            UpdatedAt = task.UpdatedAt,
            ETag = ETagGenerator.From(task.Id.ToString(), task.UpdatedAt)
        };

        return Result<TaskResponse>.Success(response);
    }
}
