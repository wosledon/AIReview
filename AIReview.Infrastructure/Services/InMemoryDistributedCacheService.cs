using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using AIReview.Core.Interfaces;

namespace AIReview.Infrastructure.Services;

/// <summary>
/// 内存版分布式缓存服务(降级方案)
/// 注意: 此实现仅适用于单实例部署,不支持真正的分布式场景
/// </summary>
public class InMemoryDistributedCacheService : IDistributedCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<InMemoryDistributedCacheService> _logger;
    
    // 内存锁字典(仅限单实例有效)
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private static readonly ConcurrentDictionary<string, string> _lockTokens = new();

    public InMemoryDistributedCacheService(
        IDistributedCache cache,
        ILogger<InMemoryDistributedCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _cache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(data))
                return default;

            return JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }

            await _cache.SetStringAsync(key, data, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _cache.GetStringAsync(key, cancellationToken);
            return !string.IsNullOrEmpty(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence {Key}", key);
            return false;
        }
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key {Key}", key);
            return false;
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
            return cached;

        var value = await factory();
        await SetAsync(key, value, expiration, cancellationToken);
        return value;
    }

    public async Task<IDistributedLock?> TryAcquireLockAsync(
        string key,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            var acquired = await semaphore.WaitAsync(timeout, cancellationToken);
            
            if (acquired)
            {
                var token = Guid.NewGuid().ToString();
                _lockTokens[key] = token;
                
                _logger.LogDebug("Lock acquired for key {Key} with token {Token} (in-memory)", key, token);
                return new InMemoryDistributedLock(key, token, this, _logger);
            }
            
            _logger.LogWarning("Failed to acquire lock for key {Key} within {Timeout}", key, timeout);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock for key {Key}", key);
            return null;
        }
    }

    private async Task ReleaseLockAsync(string key, string token)
    {
        try
        {
            if (_lockTokens.TryGetValue(key, out var currentToken) && currentToken == token)
            {
                _lockTokens.TryRemove(key, out _);
                
                if (_locks.TryGetValue(key, out var semaphore))
                {
                    semaphore.Release();
                    _logger.LogDebug("Lock released for key {Key} with token {Token}", key, token);
                }
            }
            else
            {
                _logger.LogWarning("Attempted to release lock {Key} with invalid token {Token}", key, token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock for key {Key}", key);
        }
        
        await Task.CompletedTask;
    }

    public async Task<bool> HashSetAsync(string key, string field, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            // 简化实现: 使用嵌套的JSON存储Hash
            var hashKey = $"{key}:hash";
            var hash = await GetAsync<Dictionary<string, string>>(hashKey, cancellationToken) 
                       ?? new Dictionary<string, string>();
            
            hash[field] = value;
            await SetAsync(hashKey, hash, null, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting hash field {Key}:{Field}", key, field);
            return false;
        }
    }

    public async Task<bool> HashSetAsync<T>(string key, string field, T value, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            return await HashSetAsync(key, field, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting hash field {Key}:{Field}", key, field);
            return false;
        }
    }

    public async Task<string?> HashGetAsync(string key, string field, CancellationToken cancellationToken = default)
    {
        try
        {
            var hashKey = $"{key}:hash";
            var hash = await GetAsync<Dictionary<string, string>>(hashKey, cancellationToken);
            return hash?.TryGetValue(field, out var value) == true ? value : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hash field {Key}:{Field}", key, field);
            return null;
        }
    }

    public async Task<T?> HashGetAsync<T>(string key, string field, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await HashGetAsync(key, field, cancellationToken);
            if (string.IsNullOrEmpty(json))
                return default;
            
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hash field {Key}:{Field}", key, field);
            return default;
        }
    }

    public async Task<Dictionary<string, string>> HashGetAllAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var hashKey = $"{key}:hash";
            return await GetAsync<Dictionary<string, string>>(hashKey, cancellationToken) 
                   ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all hash fields {Key}", key);
            return new Dictionary<string, string>();
        }
    }

    public async Task<bool> HashDeleteAsync(string key, string field, CancellationToken cancellationToken = default)
    {
        try
        {
            var hashKey = $"{key}:hash";
            var hash = await GetAsync<Dictionary<string, string>>(hashKey, cancellationToken);
            
            if (hash != null && hash.Remove(field))
            {
                await SetAsync(hashKey, hash, null, cancellationToken);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hash field {Key}:{Field}", key, field);
            return false;
        }
    }

    public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var current = await GetAsync<long>(key, cancellationToken);
            var newValue = current + value;
            await SetAsync(key, newValue, expiry, cancellationToken);
            return newValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing key {Key}", key);
            return 0;
        }
    }

    public async Task<long> DecrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        return await IncrementAsync(key, -value, null, cancellationToken);
    }

    public async Task<long> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // 内存版本不支持模式匹配删除
        _logger.LogWarning("RemoveByPatternAsync is not supported in in-memory implementation. Pattern: {Pattern}", pattern);
        return 0;
    }

    public async Task<bool> ExpireAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _cache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(value))
                return false;

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetStringAsync(key, value, options, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting expiration for key {Key}", key);
            return false;
        }
    }

    private class InMemoryDistributedLock : IDistributedLock
    {
        private readonly string _key;
        private readonly string _token;
        private readonly InMemoryDistributedCacheService _service;
        private readonly ILogger _logger;
        private bool _disposed;

        public string Key => _key;
        public string Token => _token;
        public bool IsAcquired => !_disposed;

        public InMemoryDistributedLock(
            string key,
            string token,
            InMemoryDistributedCacheService service,
            ILogger logger)
        {
            _key = key;
            _token = token;
            _service = service;
            _logger = logger;
        }

        public async Task<bool> ExtendAsync(TimeSpan additionalTime, CancellationToken cancellationToken = default)
        {
            // 内存版本不需要延长锁,总是返回true
            _logger.LogDebug("ExtendAsync called for lock {Key}, but in-memory implementation doesn't need lock extension", _key);
            return true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                await _service.ReleaseLockAsync(_key, _token);
                _disposed = true;
            }
        }
    }
}
