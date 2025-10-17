namespace TaskService.Domain.TaskItemManagement;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<TaskItem> Tasks, long TotalCount)> GetByProjectAsync(
        string projectId,
        TaskItemStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<TaskItem> Tasks, long TotalCount)> GetByAssigneeAsync(
        string assigneeUserId,
        TaskItemStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task CreateAsync(TaskItem task, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<Dictionary<TaskItemStatus, int>> GetStatisticsByProjectAsync(
        string projectId,
        CancellationToken cancellationToken = default);
}
