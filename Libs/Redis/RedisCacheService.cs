using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace STL.SharedServices.Caching;

public sealed class RedisCacheService(
    IDistributedCache cache,
    IOptions<CacheOptions> options,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly DistributedCacheEntryOptions _entryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(
            options.Value.AbsoluteExpirationMinutes)
    };

    public async Task<string> GetVersionAsync(
        string versionKey,
        CancellationToken ct = default)
    {
        try
        {
            return await cache.GetStringAsync(versionKey, ct) ?? "0";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Unable to read Redis cache version {VersionKey}",
                versionKey);
            return "0";
        }
    }

    public async Task BumpVersionAsync(
        string versionKey,
        CancellationToken ct = default)
    {
        try
        {
            await cache.SetStringAsync(
                versionKey,
                Guid.NewGuid().ToString("N"),
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Unable to update Redis cache version {VersionKey}",
                versionKey);
        }
    }

    public async Task<T?> GetAsync<T>(
        string key,
        CancellationToken ct = default)
    {
        try
        {
            var cachedValue = await cache.GetStringAsync(key, ct);

            return cachedValue is null
                ? default
                : JsonSerializer.Deserialize<T>(cachedValue, SerializerOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Unable to read Redis cache key {CacheKey}", key);
            return default;
        }
    }

    public Task SetAsync<T>(
        string key,
        T value,
        CancellationToken ct = default)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(
                value,
                SerializerOptions);

            return SetCacheValueAsync(key, serializedValue, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Unable to serialize Redis cache key {CacheKey}", key);
            return Task.CompletedTask;
        }
    }

    public Task RemoveAsync(
        string key,
        CancellationToken ct = default)
    {
        return RemoveCacheValueAsync(key, ct);
    }

    private async Task SetCacheValueAsync(
        string key,
        string value,
        CancellationToken ct)
    {
        try
        {
            await cache.SetStringAsync(key, value, _entryOptions, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Unable to write Redis cache key {CacheKey}", key);
        }
    }

    private async Task RemoveCacheValueAsync(
        string key,
        CancellationToken ct)
    {
        try
        {
            await cache.RemoveAsync(key, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Unable to remove Redis cache key {CacheKey}", key);
        }
    }
}
