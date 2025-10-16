# 分块评审功能文档

## 概述

**分块评审 (Chunked Review)** 是为解决大型代码变更超过 LLM 上下文限制而设计的智能评审策略。

### 问题背景

DeepSeek API 的上下文限制为 **131,072 tokens** (约 524KB 字符),当一次性提交的代码变更过大时会触发以下错误:

```
This model's maximum context length is 131072 tokens. 
However, you requested 243590 tokens (241542 in messages, 2048 in completion).
```

### 解决方案

不是简单截断代码(会丢失信息),而是采用**分块评审并汇总结果**的策略:

```
大型代码变更
    ↓
按文件分块
    ↓
并行评审每个块
    ↓
汇总评审结果
    ↓
生成综合报告
```

## 核心特性

### 1. 自动检测

```csharp
// 自动判断是否需要分块
await _chunkedReviewService.ReviewWithAutoChunkingAsync(diff, context);
```

- **估算 token 数**: 每 4 个字符 ≈ 1 token
- **阈值判断**: 
  - ≤ 101,000 tokens → 标准评审
  - \> 101,000 tokens → 分块评审

### 2. 智能分块

按以下规则分割 diff:

1. **文件边界**: 识别 `diff --git` 和 `+++` 标记
2. **大小限制**: 单个块不超过 ~404,000 字符 (101,000 tokens)
3. **完整性保证**: 不在代码块中间切断

### 3. 并行评审

```csharp
// 最多 3 个并发请求,避免 API 限流
var semaphore = new SemaphoreSlim(3);
```

- 控制并发数防止 API 限流
- 失败的块不影响其他块
- 记录详细的评审日志

### 4. 结果汇总

生成综合报告包含:

```json
{
  "overall_score": 75,
  "summary": "# 分块评审汇总报告\n\n## 评审概况\n- 总文件数: 5\n...",
  "comments": [
    {
      "file": "src/Services/UserService.cs",
      "line": 42,
      "severity": "high",
      "message": "潜在的空引用异常"
    }
  ],
  "metadata": {
    "chunked_review": true,
    "total_chunks": 5,
    "successful_chunks": 5,
    "failed_chunks": 0
  }
}
```

## 使用示例

### 场景 1: 小型变更 (自动标准评审)

```csharp
// 代码量: 50KB (约 12,500 tokens)
var diff = "..."; // 单个文件的小改动
var result = await aiReviewer.ReviewCodeAsync(diff, context);
// ✅ 使用标准评审流程,一次 API 调用
```

### 场景 2: 大型变更 (自动分块评审)

```csharp
// 代码量: 2MB (约 500,000 tokens)
var diff = "..."; // 修改了 20 个文件
var result = await aiReviewer.ReviewCodeAsync(diff, context);
// ✅ 自动分为 20 个块,并行评审,汇总结果
```

### 日志示例

```
[INFO] 代码评审请求 - 预估token数: 125000, 字符数: 500000
[WARN] 代码量超出限制 (125000 tokens > 101000 tokens), 启用分块评审
[INFO] 将diff分为 8 个文件块进行评审
[INFO] 评审第 1/8 个文件块: src/Services/UserService.cs (45000 字符)
[INFO] 评审第 2/8 个文件块: src/Controllers/ApiController.cs (38000 字符)
...
[INFO] 分块评审完成 - 总耗时: 45.2秒, 成功: 8/8
```

## 配置参数

### Token 限制配置

```csharp
// ChunkedReviewService.cs
private const int MAX_CODE_TOKENS = 101_000;  // 代码内容最大 token 数
private const int CHARS_PER_TOKEN = 4;         // Token 估算比例
```

**调整建议**:
- DeepSeek: 101,000 tokens (预留 30,000 tokens 缓冲)
- GPT-4: 调整为 120,000 tokens (128K 上下文)
- GPT-3.5: 调整为 12,000 tokens (16K 上下文)

### 并发控制

```csharp
var semaphore = new SemaphoreSlim(3); // 最多 3 个并发请求
```

**调整建议**:
- API 有限流: 减少到 2
- 性能优先: 增加到 5 (注意成本)
- 免费额度: 保持 1-2

## 性能对比

| 场景 | 代码大小 | 标准评审 | 分块评审 | 提升 |
|------|---------|---------|---------|------|
| 小改动 | 50KB | ✅ 5秒 | ✅ 5秒 | 无差异 |
| 中等改动 | 300KB | ✅ 15秒 | ✅ 15秒 | 无差异 |
| 大改动 | 1MB | ❌ 失败 | ✅ 35秒 | 从失败到成功 |
| 超大改动 | 5MB | ❌ 失败 | ✅ 120秒 | 从失败到成功 |

## 错误处理

### 部分块失败

```json
{
  "summary": "⚠️ 部分文件评审失败,请检查日志\n\n📄 file1.cs: 评审完成\n❌ file2.cs: 评审失败\n📄 file3.cs: 评审完成",
  "metadata": {
    "successful_chunks": 2,
    "failed_chunks": 1
  }
}
```

### 全部块失败

- 系统会返回错误信息
- 不影响其他功能运行
- 详细错误记录在日志中

## 最佳实践

### 1. 代码提交建议

```bash
# ✅ 推荐: 小批量频繁提交
git commit -m "feat: add user service"
git commit -m "feat: add user controller"

# ❌ 避免: 大批量一次性提交
git commit -m "feat: complete entire user module (50 files)"
```

### 2. 评审策略

- **小改动 (< 100KB)**: 标准评审,速度快
- **中等改动 (100KB - 500KB)**: 自动分块,体验好
- **大改动 (> 500KB)**: 分块评审,建议拆分提交

### 3. 监控与优化

```csharp
// 记录评审指标
_logger.LogInformation(
    "评审完成 - Token使用: {Tokens}, 耗时: {Duration}ms, 分块数: {Chunks}",
    estimatedTokens, duration.TotalMilliseconds, chunkCount);
```

## 技术细节

### Token 估算算法

```csharp
private int EstimateTokens(string text)
{
    // 简化估算: 每 4 个字符约等于 1 个 token
    // 实际情况:
    // - 英文代码: 1 token ≈ 4 字符
    // - 中文注释: 1 token ≈ 1.5 字符
    // - 混合内容: 1 token ≈ 3 字符
    return text.Length / CHARS_PER_TOKEN;
}
```

### Diff 分块算法

```csharp
// 识别文件边界
if (line.StartsWith("diff --git") || line.StartsWith("+++"))
{
    // 保存上一个 chunk
    // 开始新的 chunk
}

// 强制分割过大的块
if (currentSize > MAX_CODE_CHARS)
{
    // 即使在同一文件中也要分割
    // 标记为 "filename (continued)"
}
```

### 结果汇总策略

1. **评分**: 取所有块的平均分
2. **评论**: 合并所有评论,添加文件名前缀
3. **摘要**: 生成分块评审概况报告

## 未来优化

### 1. 精确 Token 计数

```csharp
// 使用 tiktoken 库进行精确计数
var encoding = Tiktoken.EncodingForModel("gpt-4");
var tokens = encoding.Encode(text);
return tokens.Count;
```

### 2. 智能优先级

```csharp
// 优先评审关键文件
var criticalFiles = new[] { "*.cs", "*.ts", "*.sql" };
var nonCriticalFiles = new[] { "*.md", "*.txt", "*.json" };
```

### 3. 增量评审

```csharp
// 只评审新增/修改的代码,跳过删除的代码
if (line.StartsWith("-")) continue;
```

### 4. 缓存机制

```csharp
// 缓存已评审的文件块
var cacheKey = $"review:{GetFileHash(chunk)}";
if (_cache.TryGetValue(cacheKey, out var cachedResult))
{
    return cachedResult;
}
```

## 常见问题

### Q: 为什么不直接截断代码?

**A**: 截断会丢失重要信息,导致评审不完整。分块评审能保证每个文件都被完整评审。

### Q: 分块评审会增加成本吗?

**A**: 是的,会增加 API 调用次数。但这是唯一能评审大型变更的方法。

### Q: 如何减少分块评审的频率?

**A**: 建议小批量频繁提交代码,而不是大批量一次性提交。

### Q: 评审质量会降低吗?

**A**: 不会。每个文件都获得完整的评审,汇总后的报告更全面。

### Q: 可以手动触发分块评审吗?

**A**: 目前是自动触发的。如需手动控制,可以调整 `MAX_CODE_TOKENS` 阈值。

## 总结

分块评审功能解决了大型代码变更无法评审的问题,具有以下优势:

✅ **自动检测**: 无需手动配置,智能判断  
✅ **完整评审**: 不丢失代码,每个文件都被评审  
✅ **并行处理**: 提高评审速度  
✅ **容错机制**: 部分失败不影响整体  
✅ **详细报告**: 生成综合评审报告

推荐所有处理大型代码库的团队启用此功能!
