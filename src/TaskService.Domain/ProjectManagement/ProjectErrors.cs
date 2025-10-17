using TaskService.Shared;

namespace TaskService.Domain.ProjectManagement;

public static class ProjectErrors
{
    public static Error NotFound(string projectId) =>
        new("Project.NotFound", $"Project with ID '{projectId}' was not found.");

    public static Error DuplicateName(string name) =>
        new("Project.DuplicateName", $"A project with the name '{name}' already exists.");

    public static Error Deleted(string projectId) =>
        new("Project.Deleted", $"Project with ID '{projectId}' has been deleted.");

    public static Error InvalidName() =>
        new("Project.InvalidName", "Project name must be between 3 and 120 characters.");

    public static Error DescriptionTooLong() =>
        new("Project.DescriptionTooLong", "Project description cannot exceed 2000 characters.");

    public static Error Forbidden(string projectId) =>
        new("Project.Forbidden", $"You do not have permission to access project '{projectId}'.");
}
