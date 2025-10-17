using MediatR;
using TaskService.Application.Dtos.Common;
using TaskService.Application.Dtos.Tasks;
using TaskService.Domain.TaskItemManagement;
using TaskService.Shared;

namespace TaskService.Application.Tasks.Queries;

public sealed record GetTasksQuery(string ProjectId, string? Status, int PageNumber, int PageSize)
    : IRequest<Result<PaginatedList<TaskListItemResponse>>>;

public sealed class GetTasksQueryHandler(ITaskRepository taskRepository)
    : IRequestHandler<GetTasksQuery, Result<PaginatedList<TaskListItemResponse>>>
{
    public async Task<Result<PaginatedList<TaskListItemResponse>>> Handle(GetTasksQuery request,
        CancellationToken cancellationToken)
    {
        TaskItemStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(request.Status) &&
            Enum.TryParse<TaskItemStatus>(request.Status, true, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        int skip = (request.PageNumber - 1) * request.PageSize;
        (IReadOnlyList<TaskItem> tasks, long totalCount) = await taskRepository.GetByProjectAsync(
            request.ProjectId,
            statusFilter,
            skip,
            request.PageSize,
            cancellationToken);

        var items = tasks.Select(t => new TaskListItemResponse
        {
            Id = t.Id.ToString(),
            Title = t.Title,
            Status = t.Status.ToString(),
            Priority = t.Priority.ToString(),
            AssigneeUserId = t.AssigneeUserId,
            DueDate = t.DueDate,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        }).ToList();

        var paginatedList = new PaginatedList<TaskListItemResponse>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = (int)totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };

        return Result<PaginatedList<TaskListItemResponse>>.Success(paginatedList);
    }
}
