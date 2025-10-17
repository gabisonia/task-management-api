using TaskService.Shared;

namespace TaskService.Domain.TaskItemManagement;

public static class TaskErrors
{
    public static Error NotFound(string taskId) =>
        new("Task.NotFound", $"Task with ID '{taskId}' was not found.");

    public static Error Deleted(string taskId) =>
        new("Task.Deleted", $"Task with ID '{taskId}' has been deleted.");

    public static Error InvalidTitle() =>
        new("Task.InvalidTitle", "Task title must be between 3 and 160 characters.");

    public static Error DescriptionTooLong() =>
        new("Task.DescriptionTooLong", "Task description cannot exceed 4000 characters.");

    public static Error InvalidStatusTransition(string from, string to) =>
        new("Task.InvalidStatusTransition", $"Cannot transition task status from '{from}' to '{to}'.");

    public static Error NotInProject(string taskId, string projectId) =>
        new("Task.NotInProject", $"Task '{taskId}' does not belong to project '{projectId}'.");

    public static Error Forbidden(string taskId) =>
        new("Task.Forbidden", $"You do not have permission to access task '{taskId}'.");
}
