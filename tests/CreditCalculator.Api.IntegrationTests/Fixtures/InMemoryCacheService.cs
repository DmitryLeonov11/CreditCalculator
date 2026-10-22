using System.Collections.Concurrent;
using System.Text.Json;
using CreditCalculator.Application.Abstractions;

namespace CreditCalculator.Api.IntegrationTests.Fixtures;

public sealed class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, string> _store = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_store.TryGetValue(key, out var json))
        {
            return Task.FromResult(JsonSerializer.Deserialize<T>(json));
        }

        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null, CancellationToken cancellationToken = default) where T : class
    {
        _store[key] = JsonSerializer.Serialize(value);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
