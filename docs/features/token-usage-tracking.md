# Token 消耗统计功能设计文档

## 📋 功能概述

Token 消耗统计功能用于追踪和分析 AI 模型的 token 使用量和成本,帮助用户和管理员:
- 监控 AI 调用的成本
- 分析使用模式和趋势
- 优化 token 使用效率
- 进行成本预测和预算控制

## 🏗️ 架构设计

### 核心组件

1. **TokenUsageRecord**: 记录每次 LLM 调用的详细信息
2. **LLMPricingConfig**: 管理不同 LLM 提供商的定价信息
3. **TokenUsageService**: 提供 token 使用记录和统计功能
4. **TokenUsageRepository**: 数据访问层
5. **TokenUsageController**: API 端点

### 数据模型

```
TokenUsageRecord
├── Id (long)
├── UserId (string) - 用户ID
├── ProjectId (int?) - 项目ID(可选)
├── ReviewRequestId (int?) - 评审请求ID(可选)
├── LLMConfigurationId (int) - LLM配置ID
├── Provider (string) - 提供商(OpenAI, DeepSeek等)
├── Model (string) - 模型名称
├── OperationType (string) - 操作类型(Review, RiskAnalysis等)
├── PromptTokens (int) - 输入token数
├── CompletionTokens (int) - 输出token数
├── TotalTokens (int) - 总token数
├── PromptCost (decimal) - 输入成本
├── CompletionCost (decimal) - 输出成本
├── TotalCost (decimal) - 总成本
├── IsSuccessful (bool) - 是否成功
├── ErrorMessage (string?) - 错误信息
├── ResponseTimeMs (int?) - 响应时间
├── IsCached (bool) - 是否使用缓存
└── CreatedAt (DateTime) - 创建时间
```

## 💰 定价配置

### 当前支持的模型定价(每百万 tokens)

| 提供商 | 模型 | 输入价格 | 输出价格 |
|--------|------|----------|----------|
| OpenAI | gpt-4o | $5.00 | $15.00 |
| OpenAI | gpt-4o-mini | $0.15 | $0.60 |
| OpenAI | gpt-4-turbo | $10.00 | $30.00 |
| OpenAI | gpt-3.5-turbo | $0.50 | $1.50 |
| DeepSeek | deepseek-coder | $0.14 | $0.28 |
| DeepSeek | deepseek-chat | $0.14 | $0.28 |
| Azure | gpt-4o | $5.00 | $15.00 |
| Azure | gpt-4o-mini | $0.15 | $0.60 |

### 定价更新

定价信息存储在 `LLMPricingService` 静态类中,可以通过代码更新:

```csharp
LLMPricingService.AddOrUpdatePricing(new LLMPricingConfig
{
    Provider = "OpenAI",
    Model = "gpt-4o",
    PromptPricePerMillionTokens = 5.00m,
    CompletionPricePerMillionTokens = 15.00m
});
```

## 🔌 集成指南

### 1. 在 LLM 服务中集成 Token 记录

需要在每次 LLM 调用后记录 token 使用量。以下是集成示例:

#### 方法 1: 在 MultiLLMService 中集成

```csharp
public class MultiLLMService : IMultiLLMService
{
    private readonly ITokenUsageService _tokenUsageService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public async Task<string> GenerateReviewAsync(
        string code, 
        string context, 
        int? configurationId = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var configuration = await GetConfigurationAsync(configurationId);
        
        try
        {
            var provider = _providerFactory.CreateProvider(configuration);
            var fullPrompt = BuildReviewPrompt(code, context);
            
            // 估算输入 token
            var estimatedPromptTokens = _tokenUsageService.EstimateTokenCount(fullPrompt);
            
            // 调用 LLM
            var result = await provider.GenerateAsync(fullPrompt);
            stopwatch.Stop();
            
            // 估算输出 token
            var estimatedCompletionTokens = _tokenUsageService.EstimateTokenCount(result);
            
            // 记录 token 使用
            var userId = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            
            await _tokenUsageService.RecordUsageAsync(
                userId: userId,
                projectId: null, // 如果有项目上下文,传入projectId
                reviewRequestId: null, // 如果有reviewId,传入reviewRequestId
                llmConfigurationId: configuration.Id,
                provider: configuration.Provider,
                model: configuration.Model,
                operationType: "Review",
                promptTokens: estimatedPromptTokens,
                completionTokens: estimatedCompletionTokens,
                isSuccessful: true,
                responseTimeMs: (int)stopwatch.ElapsedMilliseconds
            );
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // 记录失败的调用
            var userId = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            
            await _tokenUsageService.RecordUsageAsync(
                userId: userId,
                projectId: null,
                reviewRequestId: null,
                llmConfigurationId: configuration.Id,
                provider: configuration.Provider,
                model: configuration.Model,
                operationType: "Review",
                promptTokens: 0,
                completionTokens: 0,
                isSuccessful: false,
                errorMessage: ex.Message,
                responseTimeMs: (int)stopwatch.ElapsedMilliseconds
            );
            
            throw;
        }
    }
}
```

#### 方法 2: 使用装饰器模式

创建一个装饰器来自动记录所有 LLM 调用:

```csharp
public class TokenTrackingLLMService : IMultiLLMService
{
    private readonly IMultiLLMService _innerService;
    private readonly ITokenUsageService _tokenUsageService;
    
    public async Task<string> GenerateReviewAsync(...)
    {
        return await TrackUsageAsync(
            () => _innerService.GenerateReviewAsync(...),
            "Review",
            ...
        );
    }
    
    private async Task<string> TrackUsageAsync(
        Func<Task<string>> operation,
        string operationType,
        ...)
    {
        // 记录逻辑
    }
}
```

### 2. 注册服务

在 `Program.cs` 中注册服务:

```csharp
// 注册 TokenUsageService
builder.Services.AddScoped<ITokenUsageService, TokenUsageService>();
builder.Services.AddScoped<ITokenUsageRepository, TokenUsageRepository>();

// 注册 HttpContextAccessor (用于获取当前用户)
builder.Services.AddHttpContextAccessor();
```

### 3. 数据库迁移

创建并应用数据库迁移:

```bash
cd AIReview.API
dotnet ef migrations add AddTokenUsageTracking
dotnet ef database update
```

### 4. 更新 UnitOfWork

在 `UnitOfWork.cs` 中添加 TokenUsageRepository:

```csharp
public class UnitOfWork : IUnitOfWork
{
    private ITokenUsageRepository? _tokenUsageRepository;
    
    public ITokenUsageRepository TokenUsageRepository =>
        _tokenUsageRepository ??= new TokenUsageRepository(_context, _loggerFactory.CreateLogger<TokenUsageRepository>());
}
```

## 📊 API 端点

### 用户端点

#### GET /api/v1/tokenusage/dashboard
获取用户的 token 使用仪表板

**查询参数:**
- `startDate` (可选): 开始日期
- `endDate` (可选): 结束日期
- `days` (可选, 默认30): 查询最近N天

**响应:**
```json
{
  "statistics": {
    "totalRequests": 150,
    "successfulRequests": 148,
    "failedRequests": 2,
    "totalTokens": 125000,
    "totalCost": 0.625,
    "successRate": 98.67,
    "cacheHitRate": 5.33
  },
  "providerStatistics": [
    {
      "provider": "OpenAI",
      "model": "gpt-4o-mini",
      "requestCount": 100,
      "totalTokens": 80000,
      "totalCost": 0.420
    }
  ],
  "operationStatistics": [
    {
      "operationType": "Review",
      "requestCount": 80,
      "totalTokens": 70000,
      "totalCost": 0.350
    }
  ],
  "dailyTrends": [
    {
      "date": "2025-10-17",
      "requestCount": 15,
      "totalTokens": 12000,
      "totalCost": 0.060
    }
  ],
  "recentRecords": [...]
}
```

#### GET /api/v1/tokenusage/records
获取用户的 token 使用记录

**查询参数:**
- `page` (默认1): 页码
- `pageSize` (默认50): 每页数量

#### GET /api/v1/tokenusage/projects/{projectId}/statistics
获取项目的 token 使用统计

#### POST /api/v1/tokenusage/estimate
估算 token 数量和成本

**请求体:**
```json
{
  "provider": "OpenAI",
  "model": "gpt-4o-mini",
  "text": "要分析的代码或文本",
  "estimatedCompletionTokens": 1000
}
```

**响应:**
```json
{
  "estimatedPromptTokens": 2500,
  "estimatedCompletionTokens": 1000,
  "estimatedTotalTokens": 3500,
  "estimatedPromptCost": 0.000375,
  "estimatedCompletionCost": 0.0006,
  "estimatedTotalCost": 0.000975,
  "currency": "USD"
}
```

### 管理员端点

#### GET /api/v1/tokenusage/global/statistics
获取全局 token 使用统计(仅管理员)

## 🎨 前端集成

### 1. 创建 TypeScript 类型

```typescript
// src/types/token-usage.ts
export interface TokenUsageStatistics {
  totalRequests: number;
  successfulRequests: number;
  failedRequests: number;
  totalTokens: number;
  totalCost: number;
  successRate: number;
  cacheHitRate: number;
}

export interface ProviderUsageStatistics {
  provider: string;
  model: string;
  requestCount: number;
  totalTokens: number;
  totalCost: number;
}

export interface DailyUsageTrend {
  date: string;
  requestCount: number;
  totalTokens: number;
  totalCost: number;
}

export interface TokenUsageDashboard {
  statistics: TokenUsageStatistics;
  providerStatistics: ProviderUsageStatistics[];
  operationStatistics: OperationUsageStatistics[];
  dailyTrends: DailyUsageTrend[];
  recentRecords: TokenUsageRecord[];
}
```

### 2. 创建 API 服务

```typescript
// src/services/token-usage.service.ts
import { apiClient } from './api-client';
import { TokenUsageDashboard } from '../types/token-usage';

export class TokenUsageService {
  async getDashboard(days: number = 30): Promise<TokenUsageDashboard> {
    const response = await apiClient.get<TokenUsageDashboard>(
      `/tokenusage/dashboard?days=${days}`
    );
    return response;
  }
  
  async estimateCost(provider: string, model: string, text: string): Promise<CostEstimate> {
    const response = await apiClient.post<CostEstimate>('/tokenusage/estimate', {
      provider,
      model,
      text,
      estimatedCompletionTokens: 1000
    });
    return response;
  }
}

export const tokenUsageService = new TokenUsageService();
```

### 3. 创建仪表板组件

```typescript
// src/pages/TokenUsagePage.tsx
import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { tokenUsageService } from '../services/token-usage.service';

export const TokenUsagePage: React.FC = () => {
  const { data: dashboard, isLoading } = useQuery({
    queryKey: ['token-usage-dashboard'],
    queryFn: () => tokenUsageService.getDashboard(30)
  });
  
  if (isLoading) return <div>加载中...</div>;
  
  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-6">Token 使用统计</h1>
      
      {/* 统计卡片 */}
      <div className="grid grid-cols-4 gap-4 mb-6">
        <StatCard 
          title="总请求" 
          value={dashboard.statistics.totalRequests} 
        />
        <StatCard 
          title="总 Tokens" 
          value={dashboard.statistics.totalTokens.toLocaleString()} 
        />
        <StatCard 
          title="总成本" 
          value={`$${dashboard.statistics.totalCost.toFixed(4)}`} 
        />
        <StatCard 
          title="成功率" 
          value={`${dashboard.statistics.successRate.toFixed(1)}%`} 
        />
      </div>
      
      {/* 图表 */}
      <div className="grid grid-cols-2 gap-6">
        <ProviderChart data={dashboard.providerStatistics} />
        <TrendChart data={dashboard.dailyTrends} />
      </div>
      
      {/* 最近记录 */}
      <RecentRecordsTable records={dashboard.recentRecords} />
    </div>
  );
};
```

## 📈 使用场景

### 1. 成本监控
- 实时追踪 AI 调用成本
- 设置成本预警阈值
- 生成成本报告

### 2. 性能优化
- 识别高 token 消耗的操作
- 优化 prompt 设计
- 减少不必要的 AI 调用

### 3. 用量分析
- 分析不同模型的使用情况
- 比较提供商的成本效益
- 识别使用趋势

### 4. 预算管理
- 为用户/项目设置 token 配额
- 预测未来成本
- 优化资源分配

## 🔄 未来改进

### 短期
- [ ] 添加 token 配额管理
- [ ] 实现成本预警通知
- [ ] 支持从 LLM API 响应中获取准确的 token 计数
- [ ] 添加导出报表功能

### 中期
- [ ] 实现 token 缓存策略
- [ ] 添加成本优化建议
- [ ] 支持更多 LLM 提供商
- [ ] 实现按团队的成本分摊

### 长期
- [ ] AI 驱动的成本优化
- [ ] 预测性成本分析
- [ ] 自动切换最优模型
- [ ] 成本异常检测

## 📝 注意事项

1. **Token 估算精度**: 当前使用简单的字符数/4的方式估算,实际 token 数可能有偏差。建议后续集成 tiktoken 或从 API 响应中获取准确值。

2. **定价更新**: LLM 提供商的定价可能会变化,需要定期更新 `LLMPricingService` 中的定价信息。

3. **性能考虑**: Token 记录操作应该是异步的,不应阻塞主要的 LLM 调用流程。

4. **隐私保护**: Token 使用记录可能包含敏感信息,需要适当的访问控制和数据保护措施。

5. **数据保留**: 考虑实施数据保留策略,定期归档或删除旧的使用记录。

## 🧪 测试

### 单元测试示例

```csharp
[Fact]
public async Task RecordUsageAsync_ShouldCalculateCostCorrectly()
{
    // Arrange
    var service = new TokenUsageService(_repository, _logger);
    
    // Act
    var record = await service.RecordUsageAsync(
        userId: "test-user",
        projectId: null,
        reviewRequestId: null,
        llmConfigurationId: 1,
        provider: "OpenAI",
        model: "gpt-4o-mini",
        operationType: "Review",
        promptTokens: 1000,
        completionTokens: 500
    );
    
    // Assert
    Assert.Equal(0.00015m, record.PromptCost); // $0.15 per 1M tokens
    Assert.Equal(0.0003m, record.CompletionCost); // $0.60 per 1M tokens
    Assert.Equal(0.00045m, record.TotalCost);
}
```

## 📞 支持

如有问题或建议,请联系开发团队或提交 GitHub Issue。
