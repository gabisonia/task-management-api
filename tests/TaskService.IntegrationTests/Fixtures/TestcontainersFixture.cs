using System.Collections.Immutable;
using Testcontainers.MongoDb;
using Testcontainers.Redis;
using TaskService.Application;
using TaskService.Infrastructure;

namespace TaskService.IntegrationTests.Fixtures;

public sealed class TestcontainersFixture : IAsyncLifetime
{
    private MongoDbContainer _mongo = null!;
    private RedisContainer _redis = null!;

    private string MongoConnectionString => _mongo.GetConnectionString();
    private string RedisConnectionString => _redis.GetConnectionString();

    public async Task InitializeAsync()
    {
        _mongo = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .Build();

        _redis = new RedisBuilder()
            .WithImage("redis:7.2")
            .Build();

        await _mongo.StartAsync();
        await _redis.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _mongo.StopAsync();
        await _mongo.DisposeAsync();

        await _redis.StopAsync();
        await _redis.DisposeAsync();
    }

    public ServiceProvider BuildServiceProvider(string? databaseName = null)
    {
        var dbName = databaseName ?? $"taskservice_it_{Guid.NewGuid():N}";

        var settings = new Dictionary<string, string?>
        {
            ["MongoDB:ConnectionString"] = MongoConnectionString,
            ["MongoDB:DatabaseName"] = dbName,
            ["MongoDB:CreateIndexesOnStartup"] = "false",
            ["Redis:ConnectionString"] = RedisConnectionString,
            ["Redis:DefaultExpirationMinutes"] = "30",
            ["Supabase:Audience"] = "test-audience",
            ["Supabase:Issuer"] = "http://localhost",
            ["Supabase:JwtSecret"] = "dummy-secret"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToImmutableDictionary())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}
