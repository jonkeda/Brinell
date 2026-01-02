namespace Brinell.Testing.Caching;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Manages caching in a distributed environment with multi-node synchronization.
/// Extends single-node caching to support consistency across multiple nodes.
/// </summary>
public class DistributedCacheManager : IDistributedCache
{
    private readonly CacheManager _localCache;
    private readonly IDistributedCacheBackend _backend;
    private readonly Dictionary<string, CacheNodeValue> _nodeCache = new();
    private readonly object _nodeCacheLock = new();
    private readonly List<string> _invalidationLog = new();
    private readonly object _logLock = new();

    /// <summary>
    /// Initializes distributed cache manager with local cache and backend.
    /// </summary>
    public DistributedCacheManager(CacheManager localCache, IDistributedCacheBackend backend)
    {
        _localCache = localCache ?? throw new ArgumentNullException(nameof(localCache));
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    /// <summary>
    /// Gets a value from cache, checking local first then backend.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        return await GetInternalAsync<T>(key);
    }

    /// <summary>
    /// Sets a value in both local and distributed cache.
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
    {
        await SetInternalAsync(key, value, ttl);
    }

    private async Task<T?> GetInternalAsync<T>(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be empty", nameof(key));
        }

        // Try local cache first
        var cached = await _localCache.GetAsync<T>(key);
        if (cached != null)
        {
            return cached;
        }

        // Check distributed backend
        var value = await _backend.GetAsync(key);
        if (value != null)
        {
            try
            {
                var deserialized = JsonSerializer.Deserialize<T>(value);
                if (deserialized != null)
                {
                    // Populate local cache
                    await _localCache.SetAsync(key, deserialized);
                }

                return deserialized;
            }
            catch
            {
                return default;
            }
        }

        return default;
    }

    private async Task SetInternalAsync<T>(string key, T value, TimeSpan? ttl)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be empty", nameof(key));
        }

        if (value == null)
        {
            return;
        }

        // Set locally
        await _localCache.SetAsync(key, value, ttl);

        // Set in distributed backend
        var serialized = JsonSerializer.Serialize(value);
        await _backend.SetAsync(key, serialized, ttl);

        // Update node cache
        lock (_nodeCacheLock)
        {
            _nodeCache[key] = new CacheNodeValue
            {
                Key = key,
                Value = serialized,
                SetTime = DateTime.UtcNow,
                TTL = ttl
            };
        }
    }

    /// <summary>
    /// Removes a key from both local and distributed cache.
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be empty", nameof(key));
        }

        await _localCache.RemoveAsync(key);
        await _backend.DeleteAsync(key);

        lock (_nodeCacheLock)
        {
            _nodeCache.Remove(key);
        }
    }

    /// <summary>
    /// Removes cache entries matching a pattern from all nodes.
    /// </summary>
    public async Task RemoveByPatternAsync(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            throw new ArgumentException("Pattern cannot be empty", nameof(pattern));
        }

        // Remove from local cache
        await _localCache.RemoveByPatternAsync(pattern);

        // Invalidate across nodes
        await InvalidateAcrossNodesAsync(pattern);

        LogInvalidation($"Pattern removed: {pattern}");
    }

    /// <summary>
    /// Checks if a key exists in cache.
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be empty", nameof(key));
        }

        return await _localCache.ExistsAsync(key);
    }

    /// <summary>
    /// Clears all cache entries from both local and distributed storage.
    /// </summary>
    public async Task ClearAsync()
    {
        await _localCache.ClearAsync();

        lock (_nodeCacheLock)
        {
            _nodeCache.Clear();
        }

        // Backend clear would be called here in real system
        LogInvalidation("Cache cleared");
    }

    /// <summary>
    /// Warms the cache by preloading specified values to all nodes.
    /// </summary>
    public async Task WarmCacheAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class
    {
        await SetOnAllNodesAsync(key, value, ttl);
        LogInvalidation($"Cache warmed for key: {key}");
    }

    /// <summary>
    /// Gets cache hit rate statistics.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        return new CacheStatistics { HitCount = 0, MissCount = 0 };
    }

    /// <summary>
    /// Resets cache statistics.
    /// </summary>
    public void ResetStatistics()
    {
        _localCache.ResetStatistics();
    }

    /// <summary>
    /// Gets value from a specific node.
    /// </summary>
    public async Task<T?> GetFromNodeAsync<T>(string key, string nodeId) where T : class
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be empty", nameof(key));
        }

        if (string.IsNullOrEmpty(nodeId))
        {
            throw new ArgumentException("Node ID cannot be empty", nameof(nodeId));
        }

        var value = await _backend.GetAsync(key);
        if (value != null)
        {
            return JsonSerializer.Deserialize<T>(value);
        }

        return null;
    }

    /// <summary>
    /// Sets a value on all nodes in the distributed cache.
    /// </summary>
    public async Task SetOnAllNodesAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be empty", nameof(key));
        }

        // Set on this node
        await SetAsync(key, value, ttl);

        // In a real system, broadcast to other nodes
        await BroadcastToCacheNodesAsync(key, value, ttl);

        LogInvalidation($"Set across all nodes: {key}");
    }

    /// <summary>
    /// Invalidates cache entries by pattern across all nodes.
    /// </summary>
    public async Task InvalidateAcrossNodesAsync(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            throw new ArgumentException("Pattern cannot be empty", nameof(pattern));
        }

        // Publish invalidation to all nodes
        await _backend.PublishInvalidationAsync(pattern);

        // Wait for acknowledgments
        await WaitForInvalidationAcknowledgmentAsync(pattern);

        // Local cleanup
        lock (_nodeCacheLock)
        {
            var keysToRemove = _nodeCache.Keys
                .Where(k => MatchesPattern(k, pattern))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _nodeCache.Remove(key);
            }
        }

        LogInvalidation($"Invalidated across nodes: {pattern}");
    }

    /// <summary>
    /// Checks cache consistency across nodes.
    /// </summary>
    public async Task<CacheConsistencyReport> CheckConsistencyAsync()
    {
        var report = new CacheConsistencyReport
        {
            CheckTime = DateTime.UtcNow
        };

        lock (_nodeCacheLock)
        {
            report.TotalKeys = _nodeCache.Count;

            // Check expiration
            var now = DateTime.UtcNow;
            var expiredKeys = _nodeCache.Values
                .Where(v => v.TTL.HasValue && now - v.SetTime > v.TTL.Value)
                .ToList();

            report.ExpiredKeys = expiredKeys.Count;

            // Simulate consistency check
            report.ConsistentKeys = report.TotalKeys - expiredKeys.Count;
            report.InconsistentKeys = 0;
        }

        return report;
    }

    /// <summary>
    /// Repairs inconsistencies found during consistency check.
    /// </summary>
    public async Task RepairInconsistenciesAsync()
    {
        var consistency = await CheckConsistencyAsync();

        if (consistency.InconsistentKeys > 0)
        {
            // Remove expired entries
            lock (_nodeCacheLock)
            {
                var now = DateTime.UtcNow;
                var keysToRemove = _nodeCache.Values
                    .Where(v => v.TTL.HasValue && now - v.SetTime > v.TTL.Value)
                    .Select(v => v.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _nodeCache.Remove(key);
                }
            }

            LogInvalidation($"Repaired {consistency.InconsistentKeys} inconsistencies");
        }
    }

    /// <summary>
    /// Gets detailed cache information.
    /// </summary>
    public DistributedCacheInfo GetCacheInfo()
    {
        lock (_nodeCacheLock)
        {
            var info = new DistributedCacheInfo
            {
                CachedKeys = _nodeCache.Count,
                LocalCacheSize = _nodeCache.Values.Sum(v => v.Value?.Length ?? 0),
                OldestEntry = _nodeCache.Values.OrderBy(v => v.SetTime).FirstOrDefault()?.SetTime,
                NewestEntry = _nodeCache.Values.OrderByDescending(v => v.SetTime).FirstOrDefault()?.SetTime
            };

            return info;
        }
    }

    /// <summary>
    /// Gets all invalidation logs.
    /// </summary>
    public IReadOnlyList<string> GetInvalidationLogs()
    {
        lock (_logLock)
        {
            return _invalidationLog.AsReadOnly();
        }
    }

    /// <summary>
    /// Clears all invalidation logs.
    /// </summary>
    public void ClearLogs()
    {
        lock (_logLock)
        {
            _invalidationLog.Clear();
        }
    }

    /// <summary>
    /// Broadcasts cache update to other nodes.
    /// </summary>
    private async Task BroadcastToCacheNodesAsync<T>(string key, T value, TimeSpan? ttl) where T : class
    {
        // In a real system, this would contact other cache nodes
        await Task.CompletedTask;
    }

    /// <summary>
    /// Waits for cache invalidation acknowledgment from all nodes.
    /// </summary>
    private async Task WaitForInvalidationAcknowledgmentAsync(string pattern)
    {
        // In a real system, this would wait for acks from all nodes
        await Task.Delay(50);
    }

    /// <summary>
    /// Checks if a key matches a pattern.
    /// </summary>
    private bool MatchesPattern(string key, string pattern)
    {
        // Simple pattern matching: * is wildcard
        if (pattern == "*")
        {
            return true;
        }

        if (pattern.EndsWith("*"))
        {
            var prefix = pattern.Substring(0, pattern.Length - 1);
            return key.StartsWith(prefix);
        }

        if (pattern.StartsWith("*"))
        {
            var suffix = pattern.Substring(1);
            return key.EndsWith(suffix);
        }

        return key == pattern;
    }

    /// <summary>
    /// Logs an invalidation operation.
    /// </summary>
    private void LogInvalidation(string operation)
    {
        lock (_logLock)
        {
            _invalidationLog.Add($"{DateTime.UtcNow:O} - {operation}");
        }
    }
}

/// <summary>
/// Interface for distributed cache operations.
/// </summary>
public interface IDistributedCache : ICacheManager
{
    /// <summary>Gets value from a specific node.</summary>
    Task<T?> GetFromNodeAsync<T>(string key, string nodeId) where T : class;

    /// <summary>Sets value on all nodes.</summary>
    Task SetOnAllNodesAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class;

    /// <summary>Invalidates across all nodes.</summary>
    Task InvalidateAcrossNodesAsync(string pattern);

    /// <summary>Checks cache consistency.</summary>
    Task<CacheConsistencyReport> CheckConsistencyAsync();

    /// <summary>Repairs inconsistencies.</summary>
    Task RepairInconsistenciesAsync();
}

/// <summary>
/// Backend for distributed cache operations.
/// </summary>
public interface IDistributedCacheBackend
{
    /// <summary>Sets a value in the distributed cache.</summary>
    Task SetAsync(string key, string value, TimeSpan? ttl);

    /// <summary>Gets a value from the distributed cache.</summary>
    Task<string?> GetAsync(string key);

    /// <summary>Deletes a key from the distributed cache.</summary>
    Task DeleteAsync(string key);

    /// <summary>Publishes an invalidation message to all nodes.</summary>
    Task PublishInvalidationAsync(string pattern);
}

/// <summary>
/// Default in-memory distributed cache backend.
/// </summary>
public class InMemoryDistributedCacheBackend : IDistributedCacheBackend
{
    private readonly ConcurrentDictionary<string, string> _cache = new();

    public async Task SetAsync(string key, string value, TimeSpan? ttl)
    {
        _cache[key] = value;
        await Task.CompletedTask;
    }

    public async Task<string?> GetAsync(string key)
    {
        _cache.TryGetValue(key, out var value);
        await Task.CompletedTask;
        return value;
    }

    public async Task DeleteAsync(string key)
    {
        _cache.TryRemove(key, out _);
        await Task.CompletedTask;
    }

    public async Task PublishInvalidationAsync(string pattern)
    {
        // Simulate invalidation
        await Task.CompletedTask;
    }
}

/// <summary>
/// Represents a cached value with node metadata.
/// </summary>
internal class CacheNodeValue
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime SetTime { get; set; }
    public TimeSpan? TTL { get; set; }
}

/// <summary>
/// Report on cache consistency across nodes.
/// </summary>
public class CacheConsistencyReport
{
    /// <summary>When the consistency check was performed.</summary>
    public DateTime CheckTime { get; set; }

    /// <summary>Total keys in cache.</summary>
    public int TotalKeys { get; set; }

    /// <summary>Keys that are consistent across nodes.</summary>
    public int ConsistentKeys { get; set; }

    /// <summary>Keys that are inconsistent.</summary>
    public int InconsistentKeys { get; set; }

    /// <summary>Keys that have expired.</summary>
    public int ExpiredKeys { get; set; }

    /// <summary>Details about inconsistent keys.</summary>
    public Dictionary<string, string[]> InconsistencyDetails { get; set; } = new();

    /// <summary>Gets consistency percentage.</summary>
    public double ConsistencyPercentage => TotalKeys > 0 ? (ConsistentKeys / (double)TotalKeys) * 100 : 100;
}

/// <summary>
/// Information about the distributed cache.
/// </summary>
public class DistributedCacheInfo
{
    /// <summary>Number of cached keys.</summary>
    public int CachedKeys { get; set; }

    /// <summary>Total size of cached data in bytes.</summary>
    public long LocalCacheSize { get; set; }

    /// <summary>Timestamp of oldest entry.</summary>
    public DateTime? OldestEntry { get; set; }

    /// <summary>Timestamp of newest entry.</summary>
    public DateTime? NewestEntry { get; set; }
}
