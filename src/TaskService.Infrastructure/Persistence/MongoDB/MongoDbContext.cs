using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TaskService.Domain.ProjectManagement;
using TaskService.Domain.TaskItemManagement;

namespace TaskService.Infrastructure.Persistence.MongoDB;

public sealed class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var mongoOptions = options.Value;
        var client = new MongoClient(mongoOptions.ConnectionString);
        _database = client.GetDatabase(mongoOptions.DatabaseName);
    }

    public IMongoCollection<Project> Projects => _database.GetCollection<Project>("projects");
    public IMongoCollection<TaskItem> Tasks => _database.GetCollection<TaskItem>("tasks");
    public IMongoDatabase Database => _database;
}
