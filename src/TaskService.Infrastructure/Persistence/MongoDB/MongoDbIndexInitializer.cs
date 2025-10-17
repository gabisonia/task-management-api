using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using TaskService.Domain.ProjectManagement;
using TaskService.Domain.TaskItemManagement;

namespace TaskService.Infrastructure.Persistence.MongoDB;

public sealed class MongoDbIndexInitializer : IHostedService
{
    private readonly MongoDbContext _context;
    private readonly MongoDbOptions _options;
    private readonly ILogger<MongoDbIndexInitializer> _logger;
    public MongoDbIndexInitializer(
        MongoDbContext context,
        IOptions<MongoDbOptions> options,
        ILogger<MongoDbIndexInitializer> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.CreateIndexesOnStartup)
        {
            _logger.LogInformation("Index creation on startup is disabled");
            return;
        }

        _logger.LogInformation("Creating MongoDB indexes...");

        try
        {
            await CreateProjectIndexesAsync(cancellationToken).ConfigureAwait(false);
            await CreateTaskIndexesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("MongoDB indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MongoDB indexes");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task CreateProjectIndexesAsync(CancellationToken cancellationToken)
    {
        var projectsCollection = _context.Projects;

        // Index: OwnerId + IsDeleted + CreatedAt (for list queries)
        var ownerIndexKeys = Builders<Project>.IndexKeys
            .Ascending(p => p.OwnerId)
            .Ascending(p => p.IsDeleted)
            .Descending(p => p.CreatedAt);

        var ownerIndexModel = new CreateIndexModel<Project>(
            ownerIndexKeys,
            new CreateIndexOptions { Name = "idx_owner_isdeleted_created" });

        // Partial unique index: OwnerId + Name where IsDeleted = false
        // Prevents duplicate active project names per owner while allowing soft-deleted duplicates
        var uniqueNameIndexKeys = Builders<Project>.IndexKeys
            .Ascending(p => p.OwnerId)
            .Ascending(p => p.Name);

        var uniqueNameIndexModel = new CreateIndexModel<Project>(
            uniqueNameIndexKeys,
            new CreateIndexOptions<Project>
            {
                Name = "idx_owner_name_unique",
                Unique = true,
                PartialFilterExpression = Builders<Project>.Filter.Eq(p => p.IsDeleted, false)
            });

        await projectsCollection.Indexes.CreateManyAsync(
            new[] { ownerIndexModel, uniqueNameIndexModel },
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Project indexes created");
    }

    private async Task CreateTaskIndexesAsync(CancellationToken cancellationToken)
    {
        var tasksCollection = _context.Tasks;

        // Index: ProjectId + IsDeleted + CreatedAt (for task list by project)
        var projectIndexKeys = Builders<TaskItem>.IndexKeys
            .Ascending(t => t.ProjectId)
            .Ascending(t => t.IsDeleted)
            .Descending(t => t.CreatedAt);

        var projectIndexModel = new CreateIndexModel<TaskItem>(
            projectIndexKeys,
            new CreateIndexOptions { Name = "idx_project_isdeleted_created" });

        // Index: AssigneeUserId + Status (for assigned tasks queries)
        var assigneeIndexKeys = Builders<TaskItem>.IndexKeys
            .Ascending(t => t.AssigneeUserId)
            .Ascending(t => t.Status);

        var assigneeIndexModel = new CreateIndexModel<TaskItem>(
            assigneeIndexKeys,
            new CreateIndexOptions { Name = "idx_assignee_status" });

        await tasksCollection.Indexes.CreateManyAsync(
            new[] { projectIndexModel, assigneeIndexModel },
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Task indexes created");
    }
}
