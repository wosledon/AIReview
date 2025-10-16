# Redis分布式缓存集成指南

## 概述

本文档说明如何在AIReview系统中集成和使用Redis分布式缓存,解决以下问题:
1. Job重复执行问题
2. 多实例部署时的缓存一致性
3. 分布式锁和幂等性保证
4. AI分析结果缓存

## 架构设计

### 核心组件

1. **IDistributedCacheService** - 分布式缓存服务接口
   - 基础缓存操作(Get/Set/Remove)
   - 分布式锁支持
   - 哈希操作
   - 计数器(原子操作)

2. **IJobIdempotencyService** - Job幂等性服务
   - Job执行状态跟踪
   - 防止重复执行
   - 进度更新
   - 超时管理

3. **RedisDistributedCacheService** - Redis实现
   - 使用StackExchange.Redis
   - 支持JSON序列化
   - Lua脚本确保原子性
   - 自动过期管理

##系统要求

### 必需的NuGet包

已在项目中包含:
- `Microsoft.Extensions.Caching.StackExchangeRedis` (v8.0.0)
- `StackExchange.Redis` (作为依赖自动安装)

### Redis服务器

- Redis 5.0+
- 推荐 Redis 6.0+ (支持ACL)

## 配置步骤

### 1. 更新appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=AIReview.db",
    "RedisConnection": "localhost:6379,password=your_redis_password,abortConnect=false,connectTimeout=5000,syncTimeout=5000"
  },
  "Redis": {
    "Configuration": "localhost:6379",
    "InstanceName": "AIReview:",
    "DefaultExpiry": "01:00:00",
    "LockTimeout": "00:30:00",
    "EnableLogging": true
  }
}
```

### 2. 更新Program.cs

在`Program.cs`中添加以下配置:

```csharp
// Redis配置
var redisConfiguration = builder.Configuration.GetConnectionString("RedisConnection") 
    ?? builder.Configuration["Redis:Configuration"]
    ?? "localhost:6379";

// 注册Redis连接
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConfiguration);
    configuration.AbortOnConnectFail = false;
    configuration.ConnectTimeout = 5000;
    configuration.SyncTimeout = 5000;
    configuration.ConnectRetry = 3;
    configuration.KeepAlive = 60;
    
    return ConnectionMultiplexer.Connect(configuration);
});

// 注册分布式缓存
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConfiguration;
    options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "AIReview:";
});

// 注册自定义服务
builder.Services.AddSingleton<IDistributedCacheService, RedisDistributedCacheService>();
builder.Services.AddSingleton<IJobIdempotencyService, JobIdempotencyService>();
```

### 3. Docker Compose配置(可选)

如果使用Docker部署,在`docker-compose.yml`中添加Redis:

```yaml
version: '3.8'

services:
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    command: redis-server --appendonly yes --requirepass your_redis_password
    volumes:
      - redis-data:/data
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 30s
      timeout: 3s
      retries: 3

  aireviewer-api:
    # ... 现有配置
    depends_on:
      redis:
        condition: service_healthy
    environment:
      - ConnectionStrings__RedisConnection=redis:6379,password=your_redis_password

volumes:
  redis-data:
```

## 使用指南

### 1. Job幂等性使用

在后台Job中使用幂等性服务:

```csharp
public class AIReviewJob
{
    private readonly IJobIdempotencyService _jobIdempotencyService;
    
    public async Task ProcessReviewAsync(int reviewRequestId)
    {
        // 尝试开始执行,如果已在执行或最近完成则返回null
        await using var executionContext = await _jobIdempotencyService.TryStartExecutionAsync(
            "ai-review",  // Job类型
            reviewRequestId.ToString(),  // Job唯一键
            TimeSpan.FromMinutes(30));  // 超时时间
        
        if (executionContext == null)
        {
            // Job已在执行或最近完成,直接返回
            return;
        }
        
        try
        {
            // 更新进度
            await executionContext.UpdateProgressAsync(10, "开始处理");
            
            // 执行实际工作...
            
            await executionContext.UpdateProgressAsync(50, "AI分析中");
            
            // 如果需要更多时间,延长超时
            await executionContext.ExtendTimeoutAsync(TimeSpan.FromMinutes(10));
            
            // 更多工作...
            
            await executionContext.UpdateProgressAsync(100, "完成");
            
            // 标记成功
            await executionContext.MarkSuccessAsync(result);
        }
        catch (Exception ex)
        {
            // 标记失败
            await executionContext.MarkFailureAsync(ex.Message, ex);
            throw;
        }
        // executionContext会自动释放锁
    }
}
```

### 2. 分布式缓存使用

```csharp
public class MyService
{
    private readonly IDistributedCacheService _cacheService;
    
    // 简单的Get/Set
    public async Task<string> GetDataAsync(string key)
    {
        return await _cacheService.GetAsync<string>(key);
    }
    
    public async Task SetDataAsync(string key, string value)
    {
        await _cacheService.SetAsync(key, value, TimeSpan.FromHours(1));
    }
    
    // GetOrCreate模式(自动缓存)
    public async Task<MyData> GetOrCreateDataAsync(string key)
    {
        return await _cacheService.GetOrCreateAsync(
            key,
            async () => {
                // 如果缓存不存在,执行这个函数
                return await LoadDataFromDatabase();
            },
            TimeSpan.FromHours(1));
    }
    
    // 分布式锁
    public async Task ProcessWithLockAsync(string resourceKey)
    {
        await using var lockHandle = await _cacheService.TryAcquireLockAsync(
            resourceKey, 
            TimeSpan.FromMinutes(5));
        
        if (lockHandle == null)
        {
            // 无法获取锁,资源正被其他实例使用
            return;
        }
        
        try
        {
            // 执行需要互斥的操作
            await DoSomethingCritical();
            
            // 如果需要延长锁
            await lockHandle.ExtendAsync(TimeSpan.FromMinutes(5));
        }
        finally
        {
            // 锁会自动释放
        }
    }
    
    // 计数器(原子操作)
    public async Task<long> IncrementCounterAsync(string key)
    {
        return await _cacheService.IncrementAsync(key, 1, TimeSpan.FromDays(1));
    }
    
    // 哈希操作
    public async Task SaveUserSettingAsync(string userId, string setting, object value)
    {
        await _cacheService.HashSetAsync($"user:{userId}:settings", setting, value);
    }
}
```

### 3. AI结果缓存示例

```csharp
public class AIReviewerWithCache : IAIReviewer
{
    private readonly IAIReviewer _innerReviewer;
    private readonly IDistributedCacheService _cacheService;
    
    public async Task<AIReviewResult> ReviewCodeAsync(string diff, ReviewContext context)
    {
        // 基于diff内容生成缓存键
        var diffHash = ComputeHash(diff);
        var cacheKey = $"ai:review:{diffHash}:{context.Language}";
        
        // 尝试从缓存获取
        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () => {
                // 缓存未命中,执行实际的AI分析
                return await _innerReviewer.ReviewCodeAsync(diff, context);
            },
            TimeSpan.FromHours(24));  // 缓存24小时
    }
    
    private string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
```

## 监控和维护

### Redis监控

```bash
# 连接到Redis
redis-cli -h localhost -p 6379 -a your_password

# 查看所有键
KEYS AIReview:*

# 查看Job执行状态
KEYS AIReview:job:execution:*

# 查看分布式锁
KEYS AIReview:lock:*

# 查看内存使用
INFO memory

# 清除所有AIReview相关的键
redis-cli -h localhost -p 6379 -a your_password --scan --pattern "AIReview:*" | xargs redis-cli -h localhost -p 6379 -a your_password DEL
```

### 清理过期数据

添加定时任务清理过期的Job执行记录:

```csharp
// 在Hangfire中配置定时任务
RecurringJob.AddOrUpdate<IJobIdempotencyService>(
    "cleanup-expired-job-executions",
    service => service.CleanupExpiredExecutionsAsync(TimeSpan.FromHours(24)),
    Cron.Daily);
```

## 性能优化建议

### 1. 连接池配置

```csharp
var configuration = ConfigurationOptions.Parse(redisConfiguration);
configuration.AbortOnConnectFail = false;
configuration.ConnectTimeout = 5000;
configuration.SyncTimeout = 5000;
configuration.ConnectRetry = 3;
configuration.KeepAlive = 60;
configuration.DefaultDatabase = 0;
configuration.AllowAdmin = false;  // 生产环境禁用管理命令
```

### 2. 序列化优化

考虑使用MessagePack替代JSON以提高性能:

```bash
dotnet add package MessagePack
dotnet add package MessagePack.AspNetCoreMvcFormatter
```

### 3. 键命名约定

遵循以下约定以便管理:
- `AIReview:cache:{type}:{id}` - 缓存数据
- `AIReview:job:execution:{type}:{id}` - Job执行状态
- `AIReview:lock:{resource}` - 分布式锁
- `AIReview:counter:{name}` - 计数器

## 故障排查

### 常见问题

1. **Redis连接失败**
   ```
   解决方案:
   - 检查Redis服务是否运行: systemctl status redis
   - 检查防火墙设置
   - 验证密码配置
   - 查看日志: tail -f /var/log/redis/redis-server.log
   ```

2. **锁无法释放**
   ```
   原因: 应用崩溃导致锁未释放
   解决: 锁有自动过期时间,最多等待timeout时间后会自动释放
   手动清理: redis-cli DEL AIReview:lock:specific-key
   ```

3. **Job重复执行**
   ```
   检查点:
   - 确保所有实例使用同一Redis
   - 检查Job幂等性服务是否正确注入
   - 查看Redis中的执行记录: GET AIReview:job:execution:ai-review:123
   ```

4. **内存占用过高**
   ```
   解决方案:
   - 设置Redis最大内存: maxmemory 2gb
   - 配置淘汰策略: maxmemory-policy allkeys-lru
   - 定期清理过期数据
   - 减少缓存过期时间
   ```

## 最佳实践

1. **总是设置过期时间** - 避免内存泄漏
2. **使用有意义的键名** - 便于调试和监控
3. **合理的锁超时时间** - 避免死锁
4. **监控Redis性能** - 使用Redis Insights或类似工具
5. **备份Redis数据** - 开启AOF持久化
6. **使用连接池** - 避免频繁创建连接
7. **错误处理** - Redis不可用时的降级策略

## 安全建议

1. **使用强密码** - 至少32字符随机密码
2. **限制网络访问** - 只允许应用服务器访问
3. **禁用危险命令** - `rename-command FLUSHDB "" rename-command FLUSHALL ""`
4. **开启TLS** - 生产环境使用加密连接
5. **配置ACL** - Redis 6.0+ 使用访问控制列表

## 测试

### 单元测试示例

```csharp
[Fact]
public async Task Job_ShouldNotExecute_WhenAlreadyRunning()
{
    // Arrange
    var jobService = new JobIdempotencyService(_cacheService, _logger);
    
    // Act
    var context1 = await jobService.TryStartExecutionAsync("test", "1", TimeSpan.FromMinutes(1));
    var context2 = await jobService.TryStartExecutionAsync("test", "1", TimeSpan.FromMinutes(1));
    
    // Assert
    Assert.NotNull(context1);
    Assert.Null(context2);  // 第二次调用应该返回null
    
    await context1.DisposeAsync();
}
```

## 参考资源

- [StackExchange.Redis文档](https://stackexchange.github.io/StackExchange.Redis/)
- [Redis命令参考](https://redis.io/commands)
- [Redis最佳实践](https://redis.io/topics/admin)
- [ASP.NET Core分布式缓存](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed)

## 更新日志

- 2025-10-16: 初始版本,实现基础分布式缓存和Job幂等性功能
