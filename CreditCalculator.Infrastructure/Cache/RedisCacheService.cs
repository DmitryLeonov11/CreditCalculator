using CreditCalculator.Application.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace CreditCalculator.Infrastructure.Cache;

public sealed class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly RedisOptions _options;

    public RedisCacheService(IConnectionMultiplexer redis, IOptions<RedisOptions> options)
    {
        _database = redis.GetDatabase();
        _options = options.Value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var value = await _database.StringGetAsync(key);
        if (value.IsNullOrEmpty)
            return null;

        var json = value.ToString();
        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null, CancellationToken cancellationToken = default) where T : class
    {
        var expiry = absoluteExpirationRelativeToNow ?? TimeSpan.FromMinutes(_options.DefaultExpiryMinutes);
        var serialized = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, serialized, expiry);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(key);
    }
}