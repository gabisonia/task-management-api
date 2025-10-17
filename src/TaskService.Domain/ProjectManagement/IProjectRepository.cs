namespace TaskService.Domain.ProjectManagement;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Project> Projects, long TotalCount)> GetByOwnerAsync(
        string ownerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task CreateAsync(Project project, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Project project, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string ownerId,
        string name,
        string? excludeId = null,
        CancellationToken cancellationToken = default);
}
