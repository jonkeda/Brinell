using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brinell.Testing.Caching;

/// <summary>
/// Manages distributed caching with TTL and statistics.
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// Retrieve a value from the cache.
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Store a value in the cache.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);

    /// <summary>
    /// Remove a value from the cache.
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Remove all values matching a pattern.
    /// </summary>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Check if a key exists in the cache.
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Clear all cached values.
    /// </summary>
    Task ClearAsync();
}

/// <summary>
/// In-memory cache manager for testing.
/// </summary>
public class CacheManager : ICacheManager
{
    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly CacheStatistics _statistics = new();
    private readonly object _lockObject = new();

    /// <summary>
    /// Get cache statistics.
    /// </summary>
    public CacheStatistics Statistics
    {
        get
        {
            lock (_lockObject)
            {
                return new CacheStatistics
                {
                    HitCount = _statistics.HitCount,
                    MissCount = _statistics.MissCount,
                    SetCount = _statistics.SetCount,
                    RemoveCount = _statistics.RemoveCount,
                    InvalidationCount = _statistics.InvalidationCount
                };
            }
        }
    }

    /// <summary>
    /// Get a value from the cache.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        await Task.Delay(0);  // Simulate async operation

        lock (_lockObject)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.IsExpired)
                {
                    _cache.Remove(key);
                    _statistics.MissCount++;
                    _statistics.InvalidationCount++;
                    return default;
                }

                entry.AccessCount++;
                entry.LastAccessedAt = DateTime.UtcNow;
                _statistics.HitCount++;

                try
                {
                    return JsonSerializer.Deserialize<T>(entry.SerializedValue);
                }
                catch
                {
                    return default;
                }
            }

            _statistics.MissCount++;
            return default;
        }
    }

    /// <summary>
    /// Store a value in the cache.
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
    {
        await Task.Delay(0);  // Simulate async operation

        if (value == null)
        {
            await RemoveAsync(key);
            return;
        }

        lock (_lockObject)
        {
            var serialized = JsonSerializer.Serialize(value);
            var expiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : (DateTime?)null;

            _cache[key] = new CacheEntry
            {
                Key = key,
                SerializedValue = serialized,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                AccessCount = 0,
                LastAccessedAt = null
            };

            _statistics.SetCount++;
        }
    }

    /// <summary>
    /// Remove a value from the cache.
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        await Task.Delay(0);  // Simulate async operation

        lock (_lockObject)
        {
            if (_cache.Remove(key))
            {
                _statistics.RemoveCount++;
            }
        }
    }

    /// <summary>
    /// Remove values matching a pattern.
    /// </summary>
    public async Task RemoveByPatternAsync(string pattern)
    {
        await Task.Delay(0);  // Simulate async operation

        lock (_lockObject)
        {
            var keysToRemove = _cache.Keys
                .Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _statistics.RemoveCount++;
            }
        }
    }

    /// <summary>
    /// Check if a key exists.
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        await Task.Delay(0);  // Simulate async operation

        lock (_lockObject)
        {
            if (!_cache.TryGetValue(key, out var entry))
            {
                return false;
            }

            if (entry.IsExpired)
            {
                _cache.Remove(key);
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Clear all cached values.
    /// </summary>
    public async Task ClearAsync()
    {
        await Task.Delay(0);  // Simulate async operation

        lock (_lockObject)
        {
            var count = _cache.Count;
            _cache.Clear();
            _statistics.RemoveCount += count;
        }
    }

    /// <summary>
    /// Warm the cache with a value.
    /// </summary>
    public async Task WarmCacheAsync<T>(string key, T value, TimeSpan ttl)
    {
        await SetAsync(key, value, ttl);
    }

    /// <summary>
    /// Get cache hit rate.
    /// </summary>
    public double GetHitRate()
    {
        var total = _statistics.HitCount + _statistics.MissCount;
        return total == 0 ? 0 : (double)_statistics.HitCount / total;
    }

    /// <summary>
    /// Reset statistics.
    /// </summary>
    public void ResetStatistics()
    {
        lock (_lockObject)
        {
            _statistics.HitCount = 0;
            _statistics.MissCount = 0;
            _statistics.SetCount = 0;
            _statistics.RemoveCount = 0;
            _statistics.InvalidationCount = 0;
        }
    }

    private class CacheEntry
    {
        public required string Key { get; set; }
        public required string SerializedValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int AccessCount { get; set; }
        public DateTime? LastAccessedAt { get; set; }

        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt < DateTime.UtcNow;
    }
}

/// <summary>
/// Cache performance statistics.
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Number of cache hits.
    /// </summary>
    public long HitCount { get; set; }

    /// <summary>
    /// Number of cache misses.
    /// </summary>
    public long MissCount { get; set; }

    /// <summary>
    /// Number of set operations.
    /// </summary>
    public long SetCount { get; set; }

    /// <summary>
    /// Number of remove operations.
    /// </summary>
    public long RemoveCount { get; set; }

    /// <summary>
    /// Number of items invalidated due to TTL expiration.
    /// </summary>
    public long InvalidationCount { get; set; }

    /// <summary>
    /// Calculate hit rate (0-1).
    /// </summary>
    public double HitRate => (double)HitCount / (HitCount + MissCount);

    /// <summary>
    /// Total number of accesses.
    /// </summary>
    public long TotalAccesses => HitCount + MissCount;
}
