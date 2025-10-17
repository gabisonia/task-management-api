using MediatR;
using TaskService.Application.Dtos.Tasks;
using TaskService.Domain.TaskItemManagement;
using TaskService.Shared;

namespace TaskService.Application.Tasks.Queries;

public sealed record GetTaskByIdQuery(string Id) : IRequest<Result<TaskResponse>>;

public sealed class GetTaskByIdQueryHandler(ITaskRepository taskRepository)
    : IRequestHandler<GetTaskByIdQuery, Result<TaskResponse>>
{
    public async Task<Result<TaskResponse>> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        TaskItem? task = await taskRepository.GetByIdAsync(request.Id, cancellationToken);

        if (task == null)
        {
            return Result<TaskResponse>.Failure(new Error("TASK_NOT_FOUND", $"Task with ID '{request.Id}' not found."));
        }

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
