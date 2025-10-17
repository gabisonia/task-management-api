namespace TaskService.Infrastructure.Persistence.Redis;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public string ConnectionString { get; set; } = "localhost:6379";
    public int DefaultExpirationMinutes { get; set; } = 60;
}
