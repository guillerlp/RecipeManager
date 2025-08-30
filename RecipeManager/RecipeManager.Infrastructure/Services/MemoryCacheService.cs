using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using RecipeManager.Application.Common.Interfaces.Caching;

namespace RecipeManager.Infrastructure.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, bool> _cacheKeys;

    public MemoryCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        _cacheKeys = new ConcurrentDictionary<string, bool>();
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken token = default)
    {
        _memoryCache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, TimeSpan? sliding = null,
        CancellationToken token = default)
    {
        MemoryCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30),
            SlidingExpiration = sliding ?? TimeSpan.FromMinutes(10)
        };

        options.RegisterPostEvictionCallback((k, v, reason, state) =>
        {
            if (k is string keyString)
            {  
                _cacheKeys.TryRemove(keyString, out _);
            }
        });

        _memoryCache.Set(key, value, options);
        _cacheKeys.TryAdd(key, true);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        _memoryCache.Remove(key);
        _cacheKeys.TryRemove(key, out _);

        return Task.CompletedTask;
    }
}