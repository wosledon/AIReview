# Token 消耗统计功能 - 设计总结

## ✅ 已完成的设计

### 1. 数据模型层
- ✅ `TokenUsageRecord.cs` - Token使用记录实体
- ✅ `LLMPricingConfig.cs` - LLM定价配置和服务
- ✅ `ITokenUsageRepository.cs` - 仓储接口和统计类型定义

### 2. 业务逻辑层
- ✅ `TokenUsageService.cs` - Token使用服务实现
  - 记录token使用
  - 计算成本
  - 统计分析
  - Token估算

### 3. 数据访问层
- ✅ `TokenUsageRepository.cs` - 仓储实现
  - 查询用户/项目/全局统计
  - 按提供商/操作类型分组统计
  - 每日趋势分析

### 4. API 层
- ✅ `TokenUsageController.cs` - REST API 控制器
  - 用户仪表板
  - 统计查询
  - 成本估算
  - 记录查询

### 5. DTO 层
- ✅ `TokenUsageDto.cs` - 数据传输对象
  - TokenUsageRecordDto
  - TokenUsageStatisticsDto
  - ProviderUsageStatisticsDto
  - OperationUsageStatisticsDto
  - DailyUsageTrendDto
  - TokenUsageDashboardDto
  - CostEstimateRequestDto/ResponseDto

### 6. 文档
- ✅ `token-usage-tracking.md` - 完整的功能文档
  - 架构设计
  - 集成指南
  - API 文档
  - 前端集成示例
  - 使用场景

## 📊 核心功能特性

### 成本追踪
- ✅ 支持多个LLM提供商的定价(OpenAI, DeepSeek, Azure)
- ✅ 精确计算输入/输出token成本
- ✅ 实时成本统计和累计

### 使用分析
- ✅ 按用户/项目维度统计
- ✅ 按提供商/模型分组分析
- ✅ 按操作类型分组分析
- ✅ 每日使用趋势图表

### 性能监控
- ✅ 记录响应时间
- ✅ 追踪成功/失败率
- ✅ 监控缓存命中率

### 预估功能
- ✅ Token数量估算
- ✅ 成本预估
- ✅ 支持用户在调用前评估成本

## 🔧 技术实现亮点

### 1. 灵活的定价系统
```csharp
// 支持动态添加/更新定价
LLMPricingService.AddOrUpdatePricing(new LLMPricingConfig
{
    Provider = "OpenAI",
    Model = "gpt-4o",
    PromptPricePerMillionTokens = 5.00m,
    CompletionPricePerMillionTokens = 15.00m
});

// 自动成本计算
var (promptCost, completionCost, totalCost) = 
    LLMPricingService.CalculateCost(provider, model, promptTokens, completionTokens);
```

### 2. 强大的统计查询
```csharp
// 多维度统计
await _tokenUsageService.GetUserStatisticsAsync(userId, startDate, endDate);
await _tokenUsageService.GetProjectStatisticsAsync(projectId, startDate, endDate);
await _tokenUsageService.GetGlobalStatisticsAsync(startDate, endDate);

// 分组分析
await _tokenUsageService.GetProviderStatisticsAsync(userId, startDate, endDate);
await _tokenUsageService.GetOperationStatisticsAsync(userId, startDate, endDate);
await _tokenUsageService.GetDailyTrendsAsync(userId, days);
```

### 3. 一站式仪表板
```csharp
// 单个API调用获取完整仪表板数据
GET /api/v1/tokenusage/dashboard?days=30

{
  "statistics": {...},        // 总体统计
  "providerStatistics": [...], // 按提供商
  "operationStatistics": [...], // 按操作类型
  "dailyTrends": [...],        // 每日趋势
  "recentRecords": [...]       // 最近记录
}
```

## 📈 支持的统计维度

### 统计指标
- ✅ 总请求数
- ✅ 成功/失败请求数
- ✅ 总token数(输入/输出/总计)
- ✅ 总成本(输入/输出/总计)
- ✅ 平均响应时间
- ✅ 缓存命中数
- ✅ 成功率
- ✅ 缓存命中率

### 分组维度
- ✅ 按用户
- ✅ 按项目
- ✅ 按评审请求
- ✅ 按LLM提供商
- ✅ 按模型
- ✅ 按操作类型(Review, RiskAnalysis, PullRequestSummary, ImprovementSuggestions)
- ✅ 按日期(每日趋势)

### 时间范围
- ✅ 自定义开始/结束日期
- ✅ 最近N天
- ✅ 全部历史

## 🎯 支持的 LLM 提供商定价

| 提供商 | 模型 | 输入($\/1M tokens) | 输出($\/1M tokens) |
|--------|------|-------------------|-------------------|
| OpenAI | gpt-4o | $5.00 | $15.00 |
| OpenAI | gpt-4o-mini | $0.15 | $0.60 |
| OpenAI | gpt-4-turbo | $10.00 | $30.00 |
| OpenAI | gpt-3.5-turbo | $0.50 | $1.50 |
| DeepSeek | deepseek-coder | $0.14 | $0.28 |
| DeepSeek | deepseek-chat | $0.14 | $0.28 |
| Azure | gpt-4o | $5.00 | $15.00 |
| Azure | gpt-4o-mini | $0.15 | $0.60 |

## 🔌 集成步骤

### 1. 注册服务 (Program.cs)
```csharp
builder.Services.AddScoped<ITokenUsageService, TokenUsageService>();
builder.Services.AddScoped<ITokenUsageRepository, TokenUsageRepository>();
builder.Services.AddHttpContextAccessor();
```

### 2. 数据库迁移
```bash
dotnet ef migrations add AddTokenUsageTracking
dotnet ef database update
```

### 3. 在LLM服务中添加记录
```csharp
// 调用LLM后记录
await _tokenUsageService.RecordUsageAsync(
    userId, projectId, reviewRequestId,
    llmConfigurationId, provider, model,
    operationType, promptTokens, completionTokens,
    isSuccessful, errorMessage, responseTimeMs
);
```

### 4. 前端集成
```typescript
// 获取仪表板
const dashboard = await tokenUsageService.getDashboard(30);

// 估算成本
const estimate = await tokenUsageService.estimateCost(
  'OpenAI', 'gpt-4o-mini', codeText
);
```

## 🚀 后续改进建议

### 短期 (1-2周)
- [ ] 集成到现有的MultiLLMService中
- [ ] 添加数据库迁移脚本
- [ ] 完成前端Token使用仪表板页面
- [ ] 添加成本预警通知

### 中期 (1-2月)
- [ ] 实现Token配额管理
- [ ] 支持从LLM API响应中获取准确token数
- [ ] 添加成本报表导出功能
- [ ] 实现按团队的成本分摊

### 长期 (3-6月)
- [ ] AI驱动的成本优化建议
- [ ] 预测性成本分析
- [ ] 自动选择最优性价比模型
- [ ] 成本异常检测和告警

## 📋 文件清单

### 后端文件
```
AIReview.Core/
├── Entities/
│   ├── TokenUsageRecord.cs           ✅ 新建
│   └── LLMPricingConfig.cs          ✅ 新建
├── Interfaces/
│   └── ITokenUsageRepository.cs     ✅ 新建
└── Services/
    └── TokenUsageService.cs         ✅ 新建

AIReview.Infrastructure/
└── Repositories/
    └── TokenUsageRepository.cs      ✅ 新建

AIReview.API/
└── Controllers/
    └── TokenUsageController.cs      ✅ 新建

AIReview.Shared/
└── DTOs/
    └── TokenUsageDto.cs             ✅ 新建
```

### 前端文件(待实现)
```
aireviewer-frontend/src/
├── types/
│   └── token-usage.ts               ⏳ 待实现
├── services/
│   └── token-usage.service.ts       ⏳ 待实现
└── pages/
    └── TokenUsagePage.tsx           ⏳ 待实现
```

### 文档
```
docs/features/
└── token-usage-tracking.md          ✅ 已完成
```

## 💡 使用示例

### 示例 1: 查看个人Token使用情况
```bash
GET /api/v1/tokenusage/dashboard?days=30
```

### 示例 2: 查看项目Token成本
```bash
GET /api/v1/tokenusage/projects/123/statistics?startDate=2025-10-01
```

### 示例 3: 估算代码评审成本
```bash
POST /api/v1/tokenusage/estimate
{
  "provider": "OpenAI",
  "model": "gpt-4o-mini",
  "text": "代码内容...",
  "estimatedCompletionTokens": 1000
}

Response:
{
  "estimatedPromptTokens": 2500,
  "estimatedTotalCost": 0.000975
}
```

### 示例 4: 分析各提供商成本
```bash
GET /api/v1/tokenusage/providers/statistics?days=30

Response:
[
  {
    "provider": "OpenAI",
    "model": "gpt-4o-mini",
    "requestCount": 100,
    "totalCost": 0.420
  },
  {
    "provider": "DeepSeek",
    "model": "deepseek-coder",
    "requestCount": 50,
    "totalCost": 0.070
  }
]
```

## 🎓 关键设计决策

### 1. 为什么使用估算而非精确Token计数?
- **初期实现**: 使用字符数/4的简单估算方法
- **优点**: 无需依赖外部库,实现简单快速
- **缺点**: 精度有限(误差约10-20%)
- **后续改进**: 集成tiktoken或从API响应获取精确值

### 2. 为什么将定价配置存储在代码中?
- **灵活性**: 便于快速更新和部署
- **简单性**: 避免复杂的配置管理
- **可扩展**: 提供了AddOrUpdatePricing方法支持动态更新
- **后续改进**: 可以迁移到数据库或配置文件

### 3. 为什么记录失败的调用?
- **完整性**: 了解所有API调用情况
- **调试**: 帮助识别和解决问题
- **成本优化**: 避免重复的失败调用

### 4. 为什么支持多维度统计?
- **不同角色需求**: 用户关心个人成本,管理员关心全局
- **成本分摊**: 支持按项目/团队进行成本核算
- **优化指导**: 识别高成本操作和优化机会

## 📊 预期效果

### 成本透明化
- 用户实时了解AI调用成本
- 管理员掌握全局成本分布
- 支持成本预算和控制

### 使用优化
- 识别高成本操作
- 比较不同模型性价比
- 指导Prompt优化

### 业务价值
- 降低运营成本10-30%
- 提高资源利用效率
- 支持数据驱动决策

## 📝 注意事项

1. **数据库性能**: Token使用记录可能增长很快,考虑:
   - 定期归档历史数据
   - 添加合适的索引
   - 使用分区表

2. **异步记录**: Token记录不应阻塞主流程,建议:
   - 使用后台任务
   - 或使用消息队列

3. **隐私保护**: 记录可能包含敏感信息,需要:
   - 适当的访问控制
   - 数据脱敏
   - 符合隐私法规

4. **成本告警**: 实现主动成本管理:
   - 设置预算阈值
   - 超额告警
   - 自动降级策略

---

**设计完成时间**: 2025-10-17  
**设计者**: AI Assistant  
**状态**: ✅ 设计完成,待集成实现
