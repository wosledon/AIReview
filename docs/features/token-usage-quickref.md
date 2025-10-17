# Token 消耗统计功能 - 快速参考

## 🚀 快速开始

### 1. 记录Token使用
```csharp
await _tokenUsageService.RecordUsageAsync(
    userId: "user-id",
    projectId: 123,
    reviewRequestId: 456,
    llmConfigurationId: 1,
    provider: "OpenAI",
    model: "gpt-4o-mini",
    operationType: "Review",
    promptTokens: 2500,
    completionTokens: 1000,
    isSuccessful: true,
    responseTimeMs: 3500
);
```

### 2. 查询统计
```csharp
// 用户统计
var stats = await _tokenUsageService.GetUserStatisticsAsync(userId, startDate, endDate);

// 项目统计
var stats = await _tokenUsageService.GetProjectStatisticsAsync(projectId);

// 提供商统计
var providerStats = await _tokenUsageService.GetProviderStatisticsAsync(userId);
```

### 3. 估算成本
```csharp
// 估算Token数
var estimatedTokens = _tokenUsageService.EstimateTokenCount(code);

// 估算成本
var (promptCost, completionCost, totalCost) = 
    _tokenUsageService.EstimateCost("OpenAI", "gpt-4o-mini", 2500, 1000);
```

## 📡 API端点

| 端点 | 方法 | 说明 |
|------|------|------|
| `/api/v1/tokenusage/dashboard` | GET | 获取用户仪表板 |
| `/api/v1/tokenusage/records` | GET | 获取用户记录 |
| `/api/v1/tokenusage/projects/{id}/statistics` | GET | 获取项目统计 |
| `/api/v1/tokenusage/providers/statistics` | GET | 获取提供商统计 |
| `/api/v1/tokenusage/operations/statistics` | GET | 获取操作统计 |
| `/api/v1/tokenusage/trends/daily` | GET | 获取每日趋势 |
| `/api/v1/tokenusage/estimate` | POST | 估算成本 |
| `/api/v1/tokenusage/global/statistics` | GET | 获取全局统计(管理员) |

## 💰 定价表(每百万Tokens)

| 提供商 | 模型 | 输入 | 输出 |
|--------|------|------|------|
| OpenAI | gpt-4o | $5.00 | $15.00 |
| OpenAI | gpt-4o-mini | $0.15 | $0.60 |
| DeepSeek | deepseek-coder | $0.14 | $0.28 |

## 🔧 配置

### Program.cs
```csharp
builder.Services.AddScoped<ITokenUsageService, TokenUsageService>();
builder.Services.AddScoped<ITokenUsageRepository, TokenUsageRepository>();
builder.Services.AddHttpContextAccessor();
```

### 数据库迁移
```bash
dotnet ef migrations add AddTokenUsageTracking
dotnet ef database update
```

## 📊 统计指标

- **总请求数** (`TotalRequests`)
- **成功请求数** (`SuccessfulRequests`)
- **失败请求数** (`FailedRequests`)
- **总Token数** (`TotalTokens`)
- **总成本** (`TotalCost`)
- **成功率** (`SuccessRate`)
- **缓存命中率** (`CacheHitRate`)
- **平均响应时间** (`AverageResponseTimeMs`)

## 🎨 前端使用

```typescript
// 获取仪表板
const dashboard = await apiClient.get<TokenUsageDashboard>(
  '/tokenusage/dashboard?days=30'
);

// 估算成本
const estimate = await apiClient.post<CostEstimate>(
  '/tokenusage/estimate',
  { provider: 'OpenAI', model: 'gpt-4o-mini', text: code }
);
```

## 📝 操作类型

- `Review` - 代码评审
- `RiskAnalysis` - 风险分析
- `PullRequestSummary` - PR摘要
- `ImprovementSuggestions` - 改进建议

## 🔍 查询参数

| 参数 | 类型 | 说明 | 默认值 |
|------|------|------|--------|
| `startDate` | DateTime? | 开始日期 | null |
| `endDate` | DateTime? | 结束日期 | null |
| `days` | int | 最近天数 | 30 |
| `page` | int | 页码 | 1 |
| `pageSize` | int | 每页数量 | 50 |

## ⚡ 性能建议

1. **异步记录**: 使用后台任务记录token使用,避免阻塞主流程
2. **批量插入**: 考虑批量插入以提高性能
3. **索引优化**: 为UserId, ProjectId, CreatedAt添加索引
4. **数据归档**: 定期归档历史数据
5. **缓存统计**: 对频繁查询的统计结果进行缓存

## 🐛 常见问题

### Q: Token估算不准确?
A: 当前使用字符数/4的简单估算。建议集成tiktoken或从API响应获取准确值。

### Q: 如何处理大量记录?
A: 实施数据归档策略,使用分区表,添加适当索引。

### Q: 如何更新定价?
A: 使用 `LLMPricingService.AddOrUpdatePricing()` 方法。

### Q: 支持实时成本告警吗?
A: 当前版本不支持,计划在后续版本中添加。

## 📞 相关文档

- [完整设计文档](./token-usage-tracking.md)
- [设计总结](./token-usage-summary.md)
- [API文档](../api/token-usage-api.md)

---

**最后更新**: 2025-10-17
