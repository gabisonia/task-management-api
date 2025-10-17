using MongoDB.Bson;
using MongoDB.Driver;
using TaskService.Domain.ProjectManagement;

namespace TaskService.Infrastructure.Persistence.MongoDB.Repositories;

public sealed class MongoProjectRepository : IProjectRepository
{
    private readonly IMongoCollection<Project> _projects;
    public MongoProjectRepository(MongoDbContext context)
    {
        _projects = context.Projects;
    }
    public async Task<Project?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return null;
        }

        var filter = Builders<Project>.Filter.And(
            Builders<Project>.Filter.Eq(p => p.Id, objectId),
            Builders<Project>.Filter.Eq(p => p.IsDeleted, false));

        return await _projects.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Project> Projects, long TotalCount)> GetByOwnerAsync(
        string ownerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Project>.Filter.And(
            Builders<Project>.Filter.Eq(p => p.OwnerId, ownerId),
            Builders<Project>.Filter.Eq(p => p.IsDeleted, false));

        var sort = Builders<Project>.Sort.Descending(p => p.CreatedAt);

        var countTask = _projects.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var projectsTask = _projects
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(cancellationToken);

        await Task.WhenAll(countTask, projectsTask);

        return (projectsTask.Result, countTask.Result);
    }

    public async Task CreateAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _projects.InsertOneAsync(project, cancellationToken: cancellationToken);
    }
    public async Task<bool> UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Project>.Filter.And(
            Builders<Project>.Filter.Eq(p => p.Id, project.Id),
            Builders<Project>.Filter.Eq(p => p.IsDeleted, false));

        var result = await _projects.ReplaceOneAsync(filter, project, cancellationToken: cancellationToken);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return false;
        }

        var filter = Builders<Project>.Filter.And(
            Builders<Project>.Filter.Eq(p => p.Id, objectId),
            Builders<Project>.Filter.Eq(p => p.IsDeleted, false));

        var update = Builders<Project>.Update.Set(p => p.IsDeleted, true);

        var result = await _projects.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> ExistsByNameAsync(
        string ownerId,
        string name,
        string? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<Project>.Filter;
        var filter = filterBuilder.And(
            filterBuilder.Eq(p => p.OwnerId, ownerId),
            filterBuilder.Eq(p => p.Name, name),
            filterBuilder.Eq(p => p.IsDeleted, false));

        if (excludeId != null && ObjectId.TryParse(excludeId, out var objectId))
        {
            filter = filterBuilder.And(filter, filterBuilder.Ne(p => p.Id, objectId));
        }

        var count = await _projects.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > 0;
    }
}
