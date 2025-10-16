# Redis分布式缓存 - 快速开始指南

## 🚀 快速部署

### 步骤1: 安装Redis

#### 使用Docker (推荐)
```bash
docker run -d \
  --name aireviewer-redis \
  -p 6379:6379 \
  -v redis-data:/data \
  redis:7-alpine \
  redis-server --appendonly yes --requirepass "your_strong_password"
```

#### 或使用Docker Compose
```yaml
# docker-compose.yml
version: '3.8'
services:
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    command: redis-server --appendonly yes --requirepass your_strong_password
    volumes:
      - redis-data:/data
    restart: unless-stopped

volumes:
  redis-data:
```

运行:
```bash
docker-compose up -d redis
```

### 步骤2: 更新配置

复制示例配置到`appsettings.json`:
```bash
# 开发环境
cp docs/appsettings.redis.example.json AIReview.API/appsettings.Development.json

# 生产环境 - 记得修改密码!
cp docs/appsettings.redis.example.json AIReview.API/appsettings.Production.json
```

关键配置项:
```json
{
  "ConnectionStrings": {
    "RedisConnection": "localhost:6379,password=your_password,abortConnect=false"
  }
}
```

### 步骤3: 更新Program.cs

将`docs/redis-program-cs-config.cs`中的配置代码复制到`AIReview.API/Program.cs`的适当位置。

### 步骤4: 验证安装

运行应用:
```bash
cd AIReview.API
dotnet run
```

检查日志:
```
✓ Redis连接已建立: localhost:6379
✓ Redis连接验证成功
✓ 已配置Redis分布式缓存和Job幂等性服务
```

访问健康检查端点:
```bash
curl http://localhost:5000/health
# 应返回: Healthy
```

## 📊 监控和验证

### 验证Redis连接

```bash
# 连接到Redis
docker exec -it aireviewer-redis redis-cli -a your_password

# 测试命令
127.0.0.1:6379> PING
PONG

# 查看所有键
127.0.0.1:6379> KEYS AIReview:*

# 查看Job执行状态
127.0.0.1:6379> KEYS job:execution:*
```

### 监控Job执行

```bash
# 查看特定Job的状态
redis-cli -a your_password GET "AIReview:job:execution:ai-review:123"

# 查看所有执行中的Job
redis-cli -a your_password KEYS "AIReview:job:execution:*" | while read key; do
  echo "$key: $(redis-cli -a your_password GET $key)"
done
```

### 监控锁状态

```bash
# 查看所有锁
redis-cli -a your_password KEYS "AIReview:lock:*"

# 查看锁的值(token)
redis-cli -a your_password GET "AIReview:lock:ai-review:123"
```

## 🧪 测试幂等性

### 测试1: 防止重复执行

```bash
# 启动多个相同的Job
curl -X POST http://localhost:5000/api/v1/reviews/123/analyze &
curl -X POST http://localhost:5000/api/v1/reviews/123/analyze &
curl -X POST http://localhost:5000/api/v1/reviews/123/analyze &

# 检查日志,应该只有一个实际执行,其他的被跳过
# 日志示例:
# [ExecutionId] Starting AI review for request 123
# Skip review 123: another instance is running
# Skip review 123: another instance is running
```

### 测试2: 查看执行状态

```bash
# 在Job执行期间
redis-cli -a your_password GET "AIReview:job:execution:ai-review:123"

# 输出示例:
{
  "executionId": "abc123",
  "status": "executing",
  "startTime": "2025-10-16T10:30:00Z",
  "progressPercentage": 50,
  "progressMessage": "AI分析中..."
}
```

### 测试3: 缓存功能

```csharp
// 在代码中
var cacheService = serviceProvider.GetService<IDistributedCacheService>();

// 设置缓存
await cacheService.SetAsync("test:key", "test value", TimeSpan.FromMinutes(5));

// 读取缓存
var value = await cacheService.GetAsync<string>("test:key");
Console.WriteLine($"Cached value: {value}");

// 检查是否存在
var exists = await cacheService.ExistsAsync("test:key");
Console.WriteLine($"Key exists: {exists}");
```

## 🔧 故障排查

### 问题1: Redis连接失败

**症状**: 日志显示 "无法连接到Redis服务器"

**解决方案**:
```bash
# 1. 检查Redis是否运行
docker ps | grep redis

# 2. 检查Redis日志
docker logs aireviewer-redis

# 3. 测试连接
redis-cli -h localhost -p 6379 -a your_password ping

# 4. 检查防火墙
sudo ufw allow 6379/tcp

# 5. 验证密码
redis-cli -h localhost -p 6379 -a your_password AUTH your_password
```

### 问题2: Job仍然重复执行

**可能原因**:
1. 多个实例使用不同的Redis
2. 幂等性服务未正确注册
3. Redis密钥过期时间太短

**检查方法**:
```bash
# 1. 验证所有实例连接到同一Redis
grep "RedisConnection" appsettings.*.json

# 2. 检查服务注册
grep "IJobIdempotencyService" Program.cs

# 3. 查看Redis中的键
redis-cli -a your_password KEYS "AIReview:job:*"

# 4. 查看键的TTL
redis-cli -a your_password TTL "AIReview:job:execution:ai-review:123"
```

### 问题3: 内存占用过高

**解决方案**:
```bash
# 1. 查看内存使用
redis-cli -a your_password INFO memory

# 2. 设置最大内存(2GB)
redis-cli -a your_password CONFIG SET maxmemory 2gb
redis-cli -a your_password CONFIG SET maxmemory-policy allkeys-lru

# 3. 清理过期数据
redis-cli -a your_password --scan --pattern "AIReview:*" | \
  xargs redis-cli -a your_password DEL

# 4. 使持久化配置
docker exec aireviewer-redis redis-cli -a your_password CONFIG REWRITE
```

### 问题4: 锁无法释放

**症状**: Job一直处于执行状态

**解决方案**:
```bash
# 1. 查看锁
redis-cli -a your_password KEYS "AIReview:lock:*"

# 2. 查看锁的TTL
redis-cli -a your_password TTL "AIReview:lock:ai-review:123"

# 3. 手动删除锁(仅在确认Job已终止时)
redis-cli -a your_password DEL "AIReview:lock:ai-review:123"

# 4. 清除执行状态
redis-cli -a your_password DEL "AIReview:job:execution:ai-review:123"
```

## 🎯 性能优化

### 1. 连接池配置

在`Program.cs`中:
```csharp
var configuration = ConfigurationOptions.Parse(redisConfiguration);
configuration.ConnectTimeout = 5000;
configuration.SyncTimeout = 5000;
configuration.ConnectRetry = 3;
configuration.KeepAlive = 60;
```

### 2. 批量操作

```csharp
// 使用批量操作提高性能
var batch = db.CreateBatch();
var tasks = new List<Task>();

for (int i = 0; i < 100; i++)
{
    tasks.Add(batch.StringSetAsync($"key:{i}", $"value:{i}"));
}

batch.Execute();
await Task.WhenAll(tasks);
```

### 3. Pipeline操作

```csharp
// 使用Pipeline减少网络往返
var pipeline = db.CreateBatch();
var results = new List<Task<RedisValue>>();

for (int i = 0; i < 100; i++)
{
    results.Add(pipeline.StringGetAsync($"key:{i}"));
}

pipeline.Execute();
var values = await Task.WhenAll(results);
```

## 📈 生产环境建议

### 1. 高可用配置

使用Redis Sentinel或Redis Cluster:
```yaml
services:
  redis-master:
    image: redis:7-alpine
    command: redis-server --requirepass your_password
  
  redis-replica:
    image: redis:7-alpine
    command: redis-server --slaveof redis-master 6379 --requirepass your_password
  
  redis-sentinel:
    image: redis:7-alpine
    command: redis-sentinel /etc/redis/sentinel.conf
```

### 2. 监控

使用Redis Exporter + Prometheus + Grafana:
```yaml
services:
  redis-exporter:
    image: oliver006/redis_exporter
    environment:
      REDIS_ADDR: redis:6379
      REDIS_PASSWORD: your_password
    ports:
      - "9121:9121"
```

### 3. 备份

```bash
# 启用AOF持久化
redis-cli -a your_password CONFIG SET appendonly yes

# 手动触发RDB快照
redis-cli -a your_password BGSAVE

# 自动备份脚本
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
docker exec aireviewer-redis redis-cli -a your_password BGSAVE
docker cp aireviewer-redis:/data/dump.rdb ./backups/dump_${DATE}.rdb
```

### 4. 安全加固

```bash
# 1. 使用强密码(32+字符)
openssl rand -base64 32

# 2. 绑定到特定网络接口
redis-server --bind 127.0.0.1 10.0.0.1

# 3. 禁用危险命令
redis-server --rename-command FLUSHDB "" --rename-command FLUSHALL ""

# 4. 启用TLS
redis-server --tls-port 6379 --port 0 \
  --tls-cert-file /path/to/cert.crt \
  --tls-key-file /path/to/key.key \
  --tls-ca-cert-file /path/to/ca.crt
```

## 📚 参考链接

- [完整文档](./redis-distributed-cache-guide.md)
- [Redis官方文档](https://redis.io/docs/)
- [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/)
- [Docker Redis镜像](https://hub.docker.com/_/redis)

## 💡 提示

- ✅ 总是为缓存设置过期时间
- ✅ 使用有意义的键名前缀
- ✅ 监控Redis内存使用
- ✅ 定期备份数据
- ✅ 使用健康检查
- ❌ 不要在Redis中存储敏感信息
- ❌ 不要使用默认密码
- ❌ 不要在生产环境暴露Redis端口

## 🆘 获取帮助

遇到问题? 
1. 查看详细文档: `docs/redis-distributed-cache-guide.md`
2. 检查Redis日志: `docker logs aireviewer-redis`
3. 验证配置: `redis-cli -a your_password CONFIG GET *`
4. 测试连接: `redis-cli -a your_password PING`
