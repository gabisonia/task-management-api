namespace TaskService.Infrastructure.Persistence.MongoDB;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDB";
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "taskservice";
    public bool CreateIndexesOnStartup { get; set; } = true;
}
