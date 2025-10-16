namespace AIReview.Core.Interfaces;

/// <summary>
/// 分布式缓存服务接口
/// </summary>
public interface IDistributedCacheService
{
    /// <summary>
    /// 获取缓存值
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置缓存值
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除缓存
    /// </summary>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查键是否存在
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取或创建缓存(如果不存在则执行factory函数)
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 尝试获取分布式锁
    /// </summary>
    /// <param name="key">锁的键</param>
    /// <param name="expiry">锁的过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果获取成功返回锁令牌,否则返回null</returns>
    Task<IDistributedLock?> TryAcquireLockAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除匹配模式的键
    /// </summary>
    Task<long> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置哈希字段
    /// </summary>
    Task<bool> HashSetAsync<T>(string key, string field, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取哈希字段
    /// </summary>
    Task<T?> HashGetAsync<T>(string key, string field, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除哈希字段
    /// </summary>
    Task<bool> HashDeleteAsync(string key, string field, CancellationToken cancellationToken = default);

    /// <summary>
    /// 递增计数器(原子操作)
    /// </summary>
    Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 递减计数器(原子操作)
    /// </summary>
    Task<long> DecrementAsync(string key, long value = 1, CancellationToken cancellationToken = default);
}

/// <summary>
/// 分布式锁接口
/// </summary>
public interface IDistributedLock : IAsyncDisposable
{
    /// <summary>
    /// 锁的键
    /// </summary>
    string Key { get; }

    /// <summary>
    /// 锁的令牌(唯一标识)
    /// </summary>
    string Token { get; }

    /// <summary>
    /// 是否已获取锁
    /// </summary>
    bool IsAcquired { get; }

    /// <summary>
    /// 延长锁的过期时间
    /// </summary>
    Task<bool> ExtendAsync(TimeSpan additionalTime, CancellationToken cancellationToken = default);
}
