using AIReview.Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace AIReview.Infrastructure.Services;

/// <summary>
/// Redis分布式缓存服务实现
/// </summary>
public class RedisDistributedCacheService : IDistributedCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisDistributedCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer redis,
        ILogger<RedisDistributedCacheService> logger)
    {
        _distributedCache = distributedCache;
        _redis = redis;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _distributedCache.GetStringAsync(key, cancellationToken);
            
            if (string.IsNullOrEmpty(data))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(data, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            
            var options = new DistributedCacheEntryOptions();
            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry.Value;
            }
            else
            {
                // 默认过期时间1小时
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            }

            await _distributedCache.SetStringAsync(key, json, options, cancellationToken);
            
            _logger.LogDebug("Cache set for key: {Key}, expiry: {Expiry}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache removed for key: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence for key: {Key}", key);
            return false;
        }
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // 先尝试从缓存获取
            var cached = await GetAsync<T>(key, cancellationToken);
            if (cached != null)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cached;
            }

            _logger.LogDebug("Cache miss for key: {Key}, executing factory", key);

            // 缓存未命中,执行工厂方法
            var value = await factory();
            
            // 存入缓存
            await SetAsync(key, value, expiry, cancellationToken);
            
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrCreate for key: {Key}", key);
            throw;
        }
    }

    public async Task<IDistributedLock?> TryAcquireLockAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var lockKey = $"lock:{key}";
            var token = Guid.NewGuid().ToString("N");

            // 使用Redis的SET NX EX命令获取锁
            var acquired = await db.StringSetAsync(
                lockKey, 
                token, 
                expiry, 
                When.NotExists,
                CommandFlags.None);

            if (acquired)
            {
                _logger.LogDebug("Lock acquired for key: {Key}, token: {Token}, expiry: {Expiry}", 
                    key, token, expiry);
                return new RedisDistributedLock(db, lockKey, token, _logger);
            }

            _logger.LogDebug("Failed to acquire lock for key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock for key: {Key}", key);
            return null;
        }
    }

    public async Task<long> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            long deletedCount = 0;

            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                var keys = server.Keys(pattern: pattern);
                
                var db = _redis.GetDatabase();
                foreach (var key in keys)
                {
                    if (await db.KeyDeleteAsync(key))
                    {
                        deletedCount++;
                    }
                }
            }

            _logger.LogInformation("Deleted {Count} keys matching pattern: {Pattern}", deletedCount, pattern);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing keys by pattern: {Pattern}", pattern);
            return 0;
        }
    }

    public async Task<bool> HashSetAsync<T>(string key, string field, T value, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            await db.HashSetAsync(key, field, json);
            
            _logger.LogDebug("Hash set for key: {Key}, field: {Field}", key, field);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting hash for key: {Key}, field: {Field}", key, field);
            return false;
        }
    }

    public async Task<T?> HashGetAsync<T>(string key, string field, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.HashGetAsync(key, field);
            
            if (!value.HasValue || value.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value.ToString(), _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hash for key: {Key}, field: {Field}", key, field);
            return default;
        }
    }

    public async Task<bool> HashDeleteAsync(string key, string field, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var deleted = await db.HashDeleteAsync(key, field);
            
            _logger.LogDebug("Hash field deleted: {Deleted} for key: {Key}, field: {Field}", deleted, key, field);
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hash for key: {Key}, field: {Field}", key, field);
            return false;
        }
    }

    public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var result = await db.StringIncrementAsync(key, value);
            
            if (expiry.HasValue)
            {
                await db.KeyExpireAsync(key, expiry.Value);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing key: {Key}", key);
            throw;
        }
    }

    public async Task<long> DecrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            return await db.StringDecrementAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing key: {Key}", key);
            throw;
        }
    }
}

/// <summary>
/// Redis分布式锁实现
/// </summary>
internal class RedisDistributedLock : IDistributedLock
{
    private readonly IDatabase _database;
    private readonly ILogger _logger;
    private bool _disposed;

    public string Key { get; }
    public string Token { get; }
    public bool IsAcquired { get; private set; }

    public RedisDistributedLock(IDatabase database, string key, string token, ILogger logger)
    {
        _database = database;
        Key = key;
        Token = token;
        IsAcquired = true;
        _logger = logger;
    }

    public async Task<bool> ExtendAsync(TimeSpan additionalTime, CancellationToken cancellationToken = default)
    {
        if (_disposed || !IsAcquired)
        {
            return false;
        }

        try
        {
            // 验证令牌并延长过期时间
            var currentValue = await _database.StringGetAsync(Key);
            if (currentValue == Token)
            {
                var extended = await _database.KeyExpireAsync(Key, additionalTime);
                if (extended)
                {
                    _logger.LogDebug("Lock extended for key: {Key}, additional time: {Time}", Key, additionalTime);
                }
                return extended;
            }

            _logger.LogWarning("Lock token mismatch for key: {Key}, cannot extend", Key);
            IsAcquired = false;
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending lock for key: {Key}", Key);
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!IsAcquired)
        {
            return;
        }

        try
        {
            // 使用Lua脚本确保只有持有锁的客户端才能释放锁
            const string script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";

            var result = await _database.ScriptEvaluateAsync(
                script,
                new RedisKey[] { Key },
                new RedisValue[] { Token });

            if ((int)result == 1)
            {
                _logger.LogDebug("Lock released for key: {Key}", Key);
            }
            else
            {
                _logger.LogWarning("Lock token mismatch or already released for key: {Key}", Key);
            }

            IsAcquired = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock for key: {Key}", Key);
        }
    }
}
