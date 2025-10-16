# 分块评审与分析 - 快速开始

## 问题

之前遇到的 DeepSeek API 错误:
```
This model's maximum context length is 131072 tokens. 
However, you requested 243590 tokens
```

## 解决方案

已实现**自动分块评审与分析**功能,无需任何配置!

支持以下场景:
- ✅ **代码评审** (Code Review)
- ✅ **风险评估** (Risk Assessment)
- ✅ **改进建议** (Improvement Suggestions)
- ✅ **Pull Request分析** (PR Analysis)

## 工作原理

```
代码变更 → 自动检测大小 → 超过限制? 
                              ├─ 否 → 标准评审/分析(一次调用)
                              └─ 是 → 分块评审/分析(多次调用并汇总)
```

## 使用方法

### 完全自动,零配置

```csharp
// 1. 代码评审 - 无需修改
var result = await aiReviewer.ReviewCodeAsync(diff, context);

// 2. 风险评估 - 无需修改
var riskAssessment = await riskAssessmentService.GenerateRiskAssessmentAsync(reviewRequestId);

// 3. 改进建议 - 无需修改
var suggestions = await improvementSuggestionService.GenerateImprovementSuggestionsAsync(reviewRequestId);

// 4. PR分析 - 无需修改
var analysis = await pullRequestAnalysisService.GenerateAnalysisAsync(reviewRequestId);

// 系统会自动:
// 1. 检测代码大小
// 2. 如果超过限制,按文件分块
// 3. 并行评审/分析每个块
// 4. 汇总生成完整报告
```

### 日志输出示例

**小型变更 (标准流程)**:
```
[INFO] 代码评审请求 - 预估token数: 15000, 字符数: 60000
[INFO] 代码量在限制内,使用标准评审流程
```

**大型变更 (自动分块)**:
```
[INFO] AI分析请求 - 预估token数: 125000, 字符数: 500000
[WARN] 代码量超出限制, 启用分块分析
[INFO] 将代码分为 8 个文件块进行分析
[INFO] 分析第 1/8 个文件块: UserService.cs
[INFO] 分析第 2/8 个文件块: ApiController.cs
...
[INFO] 分块分析完成 - 总耗时: 45秒, 成功: 8/8
```

## 支持的功能

| 功能 | 方法 | 是否支持分块 |
|------|------|-------------|
| 代码评审 | `aiReviewer.ReviewCodeAsync()` | ✅ 是 |
| 风险评估 | `riskAssessmentService.GenerateRiskAssessmentAsync()` | ✅ 是 |
| 改进建议 | `improvementSuggestionService.GenerateImprovementSuggestionsAsync()` | ✅ 是 |
| PR分析 | `pullRequestAnalysisService.GenerateAnalysisAsync()` | ✅ 是 |

## 评审结果

### 标准评审结果
```json
{
  "overall_score": 85,
  "summary": "代码质量良好...",
  "comments": [...]
}
```

### 分块评审结果
```json
{
  "overall_score": 80,
  "summary": "# 分块评审汇总报告\n\n## 评审概况\n- 总文件数: 8\n- 成功评审: 8\n- 总评论数: 23",
  "comments": [
    {
      "file": "UserService.cs",
      "line": 42,
      "severity": "high",
      "message": "..."
    }
  ],
  "metadata": {
    "chunked_review": true,
    "total_chunks": 8,
    "successful_chunks": 8,
    "failed_chunks": 0
  }
}
```

**注意**: 分块评审结果中每个评论都包含 `file` 字段,方便定位问题。

## 性能影响

| 代码大小 | 处理方式 | 评审时间 | API调用次数 |
|---------|---------|---------|------------|
| < 400KB | 标准评审 | ~5-15秒 | 1次 |
| 400KB - 2MB | 分块评审(5块) | ~30-60秒 | 5次 |
| 2MB - 10MB | 分块评审(25块) | ~2-5分钟 | 25次 |

## 成本考虑

### API 调用成本
- **标准评审**: 1次 API 调用
- **分块评审**: N次 API 调用 (N = 文件数或块数)

### 优化建议
1. **小批量提交**: 建议每次提交 < 10 个文件
2. **合理拆分**: 大型功能分多个 PR 提交
3. **关注日志**: 检查是否频繁触发分块评审

## 配置 (可选)

如需调整阈值,修改 `ChunkedReviewService.cs`:

```csharp
// 当前配置 (DeepSeek)
private const int MAX_CODE_TOKENS = 101_000;  // ~400KB

// 如果使用 GPT-4 (128K context)
private const int MAX_CODE_TOKENS = 120_000;  // ~480KB

// 如果使用 GPT-3.5 (16K context)
private const int MAX_CODE_TOKENS = 12_000;   // ~48KB
```

## 最佳实践

### ✅ 推荐做法
```bash
# 小批量提交
git add src/Services/UserService.cs
git commit -m "feat: add user service"

git add src/Controllers/UserController.cs
git commit -m "feat: add user controller"
```

### ❌ 避免做法
```bash
# 大批量一次性提交
git add .
git commit -m "feat: complete entire user module (50 files, 5MB)"
```

## 故障排查

### 问题: 评审失败,显示 token 超限

**原因**: 单个文件超过限制 (> 400KB)

**解决**:
1. 检查是否有超大文件 (如自动生成的文件)
2. 将超大文件拆分为多个小文件
3. 或者排除该文件的评审

### 问题: 评审速度很慢

**原因**: 触发了分块评审,需要多次 API 调用

**解决**:
1. 减小每次提交的代码量
2. 检查是否提交了不必要的文件 (如 node_modules)
3. 使用 .gitignore 排除自动生成的文件

### 问题: 部分文件评审失败

**查看日志**:
```
[ERROR] 评审文件块 LargeFile.cs 时发生错误: Rate limit exceeded
```

**解决**:
1. 降低并发数 (修改 `SemaphoreSlim(3)` 为 `SemaphoreSlim(1)`)
2. 检查 API 限流配置
3. 稍后重试评审

## 监控

### 关键指标

在 Hangfire Dashboard 中监控:
- **评审任务数**: 查看是否有积压
- **失败率**: 是否有大量失败任务
- **平均耗时**: 是否超过 1 分钟

### 日志关键字

搜索日志中的关键信息:
```bash
# 查看分块评审触发情况
grep "启用分块评审" logs/app-*.txt

# 查看评审失败原因
grep "评审失败" logs/app-*.txt

# 统计评审耗时
grep "评审完成 - 总耗时" logs/app-*.txt
```

## 技术支持

### 查看详细文档
- [分块评审完整文档](./chunked-review-guide.md)
- [Redis 分布式缓存文档](./redis-distributed-cache-guide.md)

### 问题反馈
如遇到问题,请提供以下信息:
1. 代码变更大小 (字符数或文件数)
2. 错误日志 (从 logs/app-*.txt)
3. Hangfire Dashboard 截图

## 总结

✅ **自动化**: 无需配置,自动检测  
✅ **智能化**: 按文件分块,不丢失信息  
✅ **可靠性**: 容错机制,部分失败不影响整体  
✅ **透明性**: 详细日志,方便排查问题

**现在您可以安全地评审任意大小的代码变更!** 🎉
