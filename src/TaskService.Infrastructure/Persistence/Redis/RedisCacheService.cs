using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TaskService.Application.Abstractions;

namespace TaskService.Infrastructure.Persistence.Redis;

public sealed class RedisCacheService(
    IConnectionMultiplexer redis,
    IOptions<RedisOptions> options,
    ILogger<RedisCacheService> logger)
    : ICacheService
{
    private readonly RedisOptions _options = options.Value;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            var db = redis.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(value.ToString(), _jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting cache key {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            var db = redis.GetDatabase();
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var ttl = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

            await db.StringSetAsync(key, json, ttl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting cache key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing cache key {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = redis.GetEndPoints();
            var server = redis.GetServer(endpoints[0]);
            var db = redis.GetDatabase();

            var keys = server.Keys(pattern: pattern).ToArray();

            if (keys.Length > 0)
            {
                await db.KeyDeleteAsync(keys);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing cache keys by pattern {Pattern}", pattern);
        }
    }
}
