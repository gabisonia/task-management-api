using MongoDB.Bson;
using MongoDB.Driver;
using TaskService.Domain.TaskItemManagement;

namespace TaskService.Infrastructure.Persistence.MongoDB.Repositories;

public sealed class MongoTaskRepository : ITaskRepository
{
    private readonly IMongoCollection<TaskItem> _tasks;
    public MongoTaskRepository(MongoDbContext context)
    {
        _tasks = context.Tasks;
    }
    public async Task<TaskItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return null;
        }

        var filter = Builders<TaskItem>.Filter.And(
            Builders<TaskItem>.Filter.Eq(t => t.Id, objectId),
            Builders<TaskItem>.Filter.Eq(t => t.IsDeleted, false));

        return await _tasks.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<TaskItem> Tasks, long TotalCount)> GetByProjectAsync(
        string projectId,
        TaskItemStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(projectId, out var projectObjectId))
        {
            return (Array.Empty<TaskItem>(), 0);
        }

        var filterBuilder = Builders<TaskItem>.Filter;
        var filter = filterBuilder.And(
            filterBuilder.Eq(t => t.ProjectId, projectObjectId),
            filterBuilder.Eq(t => t.IsDeleted, false));

        if (status.HasValue)
        {
            filter = filterBuilder.And(filter, filterBuilder.Eq(t => t.Status, status.Value));
        }

        var sort = Builders<TaskItem>.Sort.Descending(t => t.CreatedAt);

        var countTask = _tasks.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var tasksTask = _tasks
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(cancellationToken);

        await Task.WhenAll(countTask, tasksTask);

        return (tasksTask.Result, countTask.Result);
    }

    public async Task<(IReadOnlyList<TaskItem> Tasks, long TotalCount)> GetByAssigneeAsync(
        string assigneeUserId,
        TaskItemStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<TaskItem>.Filter;
        var filter = filterBuilder.And(
            filterBuilder.Eq(t => t.AssigneeUserId, assigneeUserId),
            filterBuilder.Eq(t => t.IsDeleted, false));

        if (status.HasValue)
        {
            filter = filterBuilder.And(filter, filterBuilder.Eq(t => t.Status, status.Value));
        }

        var sort = Builders<TaskItem>.Sort.Descending(t => t.CreatedAt);

        var countTask = _tasks.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var tasksTask = _tasks
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(cancellationToken);

        await Task.WhenAll(countTask, tasksTask);

        return (tasksTask.Result, countTask.Result);
    }

    public async Task CreateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await _tasks.InsertOneAsync(task, cancellationToken: cancellationToken);
    }
    public async Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TaskItem>.Filter.And(
            Builders<TaskItem>.Filter.Eq(t => t.Id, task.Id),
            Builders<TaskItem>.Filter.Eq(t => t.IsDeleted, false));

        var result = await _tasks.ReplaceOneAsync(filter, task, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return false;
        }

        var filter = Builders<TaskItem>.Filter.And(
            Builders<TaskItem>.Filter.Eq(t => t.Id, objectId),
            Builders<TaskItem>.Filter.Eq(t => t.IsDeleted, false));

        var update = Builders<TaskItem>.Update.Set(t => t.IsDeleted, true);

        var result = await _tasks.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        return result.ModifiedCount > 0;
    }

    public async Task<Dictionary<TaskItemStatus, int>> GetStatisticsByProjectAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(projectId, out var projectObjectId))
        {
            return new Dictionary<TaskItemStatus, int>();
        }

        var filter = Builders<TaskItem>.Filter.And(
            Builders<TaskItem>.Filter.Eq(t => t.ProjectId, projectObjectId),
            Builders<TaskItem>.Filter.Eq(t => t.IsDeleted, false));

        var pipeline = _tasks.Aggregate()
            .Match(filter)
            .Group(t => t.Status, g => new { Status = g.Key, Count = g.Count() });

        var grouped = await pipeline.ToListAsync(cancellationToken);
        return grouped.ToDictionary(x => x.Status, x => x.Count);
    }
}
