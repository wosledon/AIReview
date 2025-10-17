using System.Text;
using System.Text.Json;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using System.Threading;

namespace AIReview.Infrastructure.Services;

/// <summary>
/// 分块评审服务 - 处理超大代码变更
/// 当代码量超过LLM上下文限制时,将代码按文件分块,分别评审/分析后汇总结果
/// 支持代码评审和AI分析两种场景
/// </summary>
public class ChunkedReviewService
{
    private readonly ILLMConfigurationService _configurationService;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILogger<ChunkedReviewService> _logger;
    private readonly ChunkedReviewOptions _options;
    
    // Token估算:粗略估计每4个字符约等于1个token(对于代码)
    private const int CHARS_PER_TOKEN = 4;
    
    // DeepSeek的token限制:131,072 tokens
    // 我们为prompt模板、系统消息、completion留出30,000 tokens的缓冲
    // 因此代码内容的最大token数为: 101,000 tokens
    private const int MAX_CODE_TOKENS = 101_000;
    private const int MAX_CODE_CHARS = MAX_CODE_TOKENS * CHARS_PER_TOKEN; // ~404,000 字符

    public ChunkedReviewService(
        ILLMConfigurationService configurationService,
        ILLMProviderFactory providerFactory,
        ILogger<ChunkedReviewService> logger,
        IOptions<ChunkedReviewOptions> options)
    {
        _configurationService = configurationService;
        _providerFactory = providerFactory;
        _logger = logger;
        _options = options?.Value ?? new ChunkedReviewOptions();
    }

    /// <summary>
    /// 智能评审:自动判断是否需要分块
    /// </summary>
    public async Task<string> ReviewWithAutoChunkingAsync(
        string diff, 
        string context, 
        int? configurationId = null)
    {
        return await ProcessWithAutoChunkingAsync(
            diff, 
            context, 
            configurationId,
            isReview: true);
    }

    /// <summary>
    /// 智能分析:自动判断是否需要分块(用于AI分析场景)
    /// </summary>
    public async Task<string> AnalyzeWithAutoChunkingAsync(
        string prompt,
        string code, 
        int? configurationId = null)
    {
        return await ProcessWithAutoChunkingAsync(
            code, 
            prompt, 
            configurationId,
            isReview: false);
    }

    /// <summary>
    /// 通用的自动分块处理
    /// </summary>
    private async Task<string> ProcessWithAutoChunkingAsync(
        string code, 
        string promptOrContext, 
        int? configurationId,
        bool isReview)
    {
        // 估算token数
        var estimatedTokens = EstimateTokens(code);
        
        _logger.LogInformation(
            "{ProcessType}请求 - 预估token数: {Tokens}, 字符数: {Chars}", 
            isReview ? "代码评审" : "AI分析",
            estimatedTokens, code.Length);

        // 如果在限制内,直接处理
        if (estimatedTokens <= MAX_CODE_TOKENS)
        {
            _logger.LogInformation("代码量在限制内,使用标准{ProcessType}流程", isReview ? "评审" : "分析");
            
            // 获取配置并调用LLM
            var configuration = await GetConfigurationAsync(configurationId);
            if (configuration == null)
            {
                throw new InvalidOperationException("没有可用的LLM配置");
            }
            
            var provider = _providerFactory.CreateProvider(configuration);
            // 优先尝试按模板占位符渲染（如果模板中包含 {{DIFF}} / {{FILE_NAME}} / {{CONTEXT}} 等）
            var rendered = TryRenderTemplate(promptOrContext, code, fileName: null, isReview: isReview);
            var prompt = rendered ?? (isReview 
                ? BuildReviewPrompt(code, promptOrContext)
                : BuildAnalysisPrompt(promptOrContext, code));
            
            return await provider.GenerateAsync(prompt);
        }

        // 超出限制,使用分块处理
        _logger.LogWarning(
            "代码量超出限制 ({EstimatedTokens} tokens > {MaxTokens} tokens), 启用分块{ProcessType}", 
            estimatedTokens, MAX_CODE_TOKENS, isReview ? "评审" : "分析");
        
        return await ProcessInChunksAsync(code, promptOrContext, configurationId, isReview);
    }

    /// <summary>
    /// 分块处理并汇总结果
    /// </summary>
    private async Task<string> ProcessInChunksAsync(
        string code, 
        string promptOrContext, 
        int? configurationId,
        bool isReview)
    {
        var startTime = DateTime.UtcNow;
        
        // 1. 将代码按文件分块
        var chunks = SplitDiffByFiles(code);
        
        _logger.LogInformation(
            "将代码分为 {ChunkCount} 个文件块进行{ProcessType}", 
            chunks.Count, isReview ? "评审" : "分析");

        // 2. 并行处理每个块(限制并发数避免API限流)
    var chunkResults = new List<ChunkReviewResult>();
    var maxConcurrency = Math.Max(1, _options.MaxConcurrency);
    var semaphore = new SemaphoreSlim(maxConcurrency); // 可配置并发
        
        var tasks = chunks.Select(async (chunk, index) =>
        {
            await semaphore.WaitAsync();
            try
            {
                var chunkPromptOrContext = isReview
                    ? $"{promptOrContext}\n\n## 当前评审文件: {chunk.FileName} (第{index + 1}/{chunks.Count}个文件)"
                    : $"{promptOrContext}\n\n## 当前分析文件: {chunk.FileName} (第{index + 1}/{chunks.Count}个文件)";
                
                _logger.LogInformation(
                    "{ProcessType}第 {Index}/{Total} 个文件块: {FileName} ({Size} 字符)",
                    isReview ? "评审" : "分析", index + 1, chunks.Count, chunk.FileName, chunk.Content.Length);
                
                // 预取配置，减少重复调用；为每个分块请求提供超时与重试
                var configuration = await GetConfigurationAsync(configurationId);
                if (configuration == null)
                {
                    throw new InvalidOperationException("没有可用的LLM配置");
                }

                // 优先按模板占位符渲染（每块替换 {{DIFF}} 为当前文件块、{{FILE_NAME}} 为文件名）
                var rendered = TryRenderTemplate(chunkPromptOrContext, chunk.Content, chunk.FileName, isReview);
                var prompt = rendered ?? (isReview 
                    ? BuildReviewPrompt(chunk.Content, chunkPromptOrContext)
                    : BuildAnalysisPrompt(chunkPromptOrContext, chunk.Content));

                var result = await ExecuteWithRetryAsync(async (ct) =>
                {
                    var provider = _providerFactory.CreateProvider(configuration);
                    // provider 层不一定支持 CancellationToken，这里只控制我们侧的超时
                    return await provider.GenerateAsync(prompt);
                },
                _options.MaxRetries,
                TimeSpan.FromMilliseconds(Math.Max(100, _options.InitialRetryDelayMs)),
                TimeSpan.FromSeconds(Math.Max(5, _options.PerChunkTimeoutSeconds)),
                isReview ? "评审" : "分析",
                chunk.FileName);

                return new ChunkReviewResult
                {
                    FileName = chunk.FileName,
                    ReviewResult = result,
                    Order = index
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ProcessType}文件块 {FileName} 时发生错误", 
                    isReview ? "评审" : "分析", chunk.FileName);
                return new ChunkReviewResult
                {
                    FileName = chunk.FileName,
                    ReviewResult = $"{{\"error\": \"{(isReview ? "评审" : "分析")}失败: {ex.Message}\"}}",
                    Order = index,
                    HasError = true
                };
            }
            finally
            {
                semaphore.Release();
            }
        });

        chunkResults = (await Task.WhenAll(tasks)).OrderBy(r => r.Order).ToList();

        // 3. 汇总处理结果
        var aggregatedResult = AggregateChunkResults(chunkResults, isReview);
        
        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "分块{ProcessType}完成 - 总耗时: {DurationSeconds}秒, 成功: {SuccessCount}/{TotalCount}",
            isReview ? "评审" : "分析",
            duration.TotalSeconds, 
            chunkResults.Count(r => !r.HasError), 
            chunkResults.Count);

        return aggregatedResult;
    }

    /// <summary>
    /// 根据占位符渲染自定义模板；当检测到模板包含 {{DIFF}} 或 {{FILE_NAME}} 或 {{CONTEXT}} 时进行替换。
    /// 若模板不包含这些占位符，则返回 null 以便走默认内置 Prompt 构建。
    /// </summary>
    private string? TryRenderTemplate(string templateOrContext, string codeOrDiff, string? fileName, bool isReview)
    {
        if (string.IsNullOrWhiteSpace(templateOrContext)) return null;

        var containsPlaceholders = templateOrContext.Contains("{{DIFF}}", StringComparison.OrdinalIgnoreCase)
            || templateOrContext.Contains("{{FILE_NAME}}", StringComparison.OrdinalIgnoreCase)
            || templateOrContext.Contains("{{CONTEXT}}", StringComparison.OrdinalIgnoreCase);

        if (!containsPlaceholders)
        {
            return null;
        }

        var rendered = templateOrContext
            .Replace("{{DIFF}}", codeOrDiff)
            .Replace("{{FILE_NAME}}", fileName ?? string.Empty);

        // {{CONTEXT}} 在上层通常已经替换过；如仍存在则留空以避免泄漏占位符
        rendered = rendered.Replace("{{CONTEXT}}", string.Empty);

        // 确保严格 JSON 输出要求（如果模板未声明，可在此追加轻量提示）
        // 为避免打断用户模板结构，这里仅在未包含明显 JSON 约束提示时，附加一行提示。
        if (!rendered.Contains("仅输出严格的 JSON", StringComparison.OrdinalIgnoreCase)
            && !rendered.Contains("Only output JSON", StringComparison.OrdinalIgnoreCase))
        {
            rendered += "\n\n请仅输出严格的 JSON（不要包含 Markdown 代码块或额外说明）。";
        }

        return rendered;
    }

    /// <summary>
    /// 按文件分割diff
    /// </summary>
    private List<DiffChunk> SplitDiffByFiles(string diff)
    {
        var chunks = new List<DiffChunk>();
        var lines = diff.Split('\n');
        
        var currentFileName = "unknown";
        var currentContent = new StringBuilder();
        var currentSize = 0;

        foreach (var line in lines)
        {
            // 检测文件边界: "diff --git a/xxx b/xxx" 或 "+++ b/xxx"
            if (line.StartsWith("diff --git") || line.StartsWith("+++"))
            {
                // 如果当前chunk不为空,保存它
                if (currentContent.Length > 0)
                {
                    chunks.Add(new DiffChunk
                    {
                        FileName = currentFileName,
                        Content = currentContent.ToString(),
                        Size = currentSize
                    });
                    
                    currentContent.Clear();
                    currentSize = 0;
                }

                // 提取新文件名
                if (line.StartsWith("diff --git"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        line, @"diff --git a/(.+?) b/(.+)");
                    if (match.Success)
                    {
                        currentFileName = match.Groups[2].Value;
                    }
                }
                else if (line.StartsWith("+++"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        line, @"\+\+\+ b/(.+)");
                    if (match.Success)
                    {
                        currentFileName = match.Groups[1].Value;
                    }
                }
            }

            // 添加行到当前chunk
            currentContent.AppendLine(line);
            currentSize += line.Length + 1; // +1 for newline

            // 如果当前chunk太大,强制分割
            if (currentSize > MAX_CODE_CHARS)
            {
                chunks.Add(new DiffChunk
                {
                    FileName = currentFileName,
                    Content = currentContent.ToString(),
                    Size = currentSize
                });
                
                currentContent.Clear();
                currentSize = 0;
                currentFileName = $"{currentFileName} (continued)";
            }
        }

        // 添加最后一个chunk
        if (currentContent.Length > 0)
        {
            chunks.Add(new DiffChunk
            {
                FileName = currentFileName,
                Content = currentContent.ToString(),
                Size = currentSize
            });
        }

        return chunks;
    }

    /// <summary>
    /// 汇总多个chunk的处理结果
    /// </summary>
    private string AggregateChunkResults(List<ChunkReviewResult> chunkResults, bool isReview)
    {
        var processType = isReview ? "评审" : "分析";
        
        try
        {
            var allComments = new List<object>();
            var allScores = new List<int>();
            var allSummaries = new List<string>();
            var hasErrors = chunkResults.Any(r => r.HasError);

            foreach (var chunk in chunkResults)
            {
                if (chunk.HasError)
                {
                    allSummaries.Add($"❌ {chunk.FileName}: {processType}失败");
                    continue;
                }

                try
                {
                    // 预处理: 从返回文本中提取有效JSON（去除 ```json 代码块、前后非JSON内容等）
                    var raw = chunk.ReviewResult ?? string.Empty;
                    if (!TryExtractJsonObject(raw, out var jsonText))
                    {
                        throw new JsonException("未能从返回内容中提取有效JSON");
                    }

                    using var json = JsonDocument.Parse(jsonText);
                    var root = json.RootElement;

                    // 提取comments（兼容根是数组或对象内的 comments 数组）
                    IEnumerable<JsonElement> commentArray = Enumerable.Empty<JsonElement>();
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        commentArray = root.EnumerateArray();
                    }
                    else if (root.TryGetProperty("comments", out var commentsProp) && commentsProp.ValueKind == JsonValueKind.Array)
                    {
                        commentArray = commentsProp.EnumerateArray();
                    }

                    foreach (var comment in commentArray)
                    {
                        var commentObj = JsonSerializer.Deserialize<Dictionary<string, object>>(comment.GetRawText());
                        if (commentObj != null)
                        {
                            commentObj["file"] = chunk.FileName;
                            allComments.Add(commentObj);
                        }
                    }

                    // 提取score（可选）
                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("overall_score", out var scoreEl))
                    {
                        if (scoreEl.ValueKind == JsonValueKind.Number && scoreEl.TryGetInt32(out var s))
                        {
                            allScores.Add(s);
                        }
                    }

                    // 提取summary（可选）
                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("summary", out var summaryEl))
                    {
                        var summaryStr = summaryEl.ToString();
                        if (!string.IsNullOrWhiteSpace(summaryStr))
                        {
                            allSummaries.Add($"📄 {chunk.FileName}: {summaryStr}");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "无法解析chunk结果为JSON: {FileName}", chunk.FileName);
                    allSummaries.Add($"⚠️ {chunk.FileName}: 解析结果失败");
                }
            }

            // 构建汇总结果
            var aggregatedScore = allScores.Any() ? (int)allScores.Average() : 0;
            var aggregatedSummary = string.Join("\n\n", allSummaries);

            var result = new
            {
                overall_score = aggregatedScore,
                summary = $@"# 分块{processType}汇总报告

## {processType}概况
- 总文件数: {chunkResults.Count}
- 成功{processType}: {chunkResults.Count(r => !r.HasError)}
- 失败{processType}: {chunkResults.Count(r => r.HasError)}
- 总评论数: {allComments.Count}
{(hasErrors ? $"\n⚠️ 部分文件{processType}失败,请检查日志" : "")}

## 各文件{processType}摘要
{aggregatedSummary}

## 总体建议
由于代码变更较大,已采用分块{processType}策略。建议重点关注高严重性(high)的评论。",
                comments = allComments,
                metadata = new
                {
                    chunked_review = true,
                    process_type = processType,
                    total_chunks = chunkResults.Count,
                    successful_chunks = chunkResults.Count(r => !r.HasError),
                    failed_chunks = chunkResults.Count(r => r.HasError)
                }
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "汇总{ProcessType}结果时发生错误", processType);
            
            // 返回简化的错误响应
            return JsonSerializer.Serialize(new
            {
                overall_score = 0,
                summary = $"分块{processType}汇总失败: {ex.Message}",
                comments = new List<object>(),
                metadata = new
                {
                    chunked_review = true,
                    process_type = processType,
                    aggregation_error = true
                }
            });
        }
    }

    /// <summary>
    /// 估算token数(粗略估计)
    /// </summary>
    private int EstimateTokens(string text)
    {
        return text.Length / CHARS_PER_TOKEN;
    }

    /// <summary>
    /// 获取LLM配置
    /// </summary>
    private async Task<LLMConfiguration?> GetConfigurationAsync(int? configurationId)
    {
        if (configurationId.HasValue)
        {
            var config = await _configurationService.GetByIdAsync(configurationId.Value);
            if (config != null && config.IsActive)
            {
                return config;
            }
        }

        // 如果没有指定ID或指定的配置不可用，使用默认配置
        return await _configurationService.GetDefaultConfigurationAsync();
    }

    /// <summary>
    /// 构建代码评审Prompt
    /// </summary>
    private string BuildReviewPrompt(string code, string context)
    {
        return $@"# 代码审查任务

你是一位资深的代码审查专家。请仔细分析以下Git差异，提供专业、详细的审查报告。

## 上下文信息
{context}

## Git差异内容
```diff
{code}
```

## 输出要求

请仅输出严格的 JSON（UTF-8，无任何 Markdown 代码块、无注释、无多余文本）。
禁止输出 ```json 或 ``` 包裹。禁止在 JSON 前后添加说明文字。
";
    }

    /// <summary>
    /// 构建AI分析Prompt
    /// </summary>
    private string BuildAnalysisPrompt(string taskPrompt, string code)
    {
        return $@"# AI 分析任务

{taskPrompt}

## 代码内容
```
{code}
```

## 输出要求

1. 仅输出严格的 JSON（UTF-8），不得包含 Markdown 代码块、注释或多余文本
2. 遵循任务要求中的字段结构
3. 确保分析深入、全面、准确
4. 提供具体的数据和证据支持你的结论
5. 如果发现问题，请提供可行的解决方案

请直接输出 JSON。";
    }

    /// <summary>
    /// 从原始文本中提取可解析的 JSON（支持 ```json 代码块、对象或数组根、以及前后噪音）
    /// </summary>
    private static bool TryExtractJsonObject(string raw, out string jsonText)
    {
        jsonText = string.Empty;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var text = raw.Trim();

        // 1) 处理 Markdown 代码块 ```json ... ``` 或 ``` ... ```
    var fenceMatch = Regex.Match(text, @"```(?:json)?\s*\n([\s\S]*?)```", RegexOptions.IgnoreCase);
        if (fenceMatch.Success && fenceMatch.Groups.Count > 1)
        {
            text = fenceMatch.Groups[1].Value.Trim();
        }

        // 2) 尝试直接解析（完整 JSON）
        if (IsValidJson(text))
        {
            jsonText = text;
            return true;
        }

        // 3) 在文本中查找首个平衡的 JSON 对象 {...}
        if (TryExtractBalancedJson(text, '{', '}', out var objJson) && IsValidJson(objJson))
        {
            jsonText = objJson;
            return true;
        }

        // 4) 在文本中查找首个平衡的 JSON 数组 [...]
        if (TryExtractBalancedJson(text, '[', ']', out var arrJson) && IsValidJson(arrJson))
        {
            jsonText = arrJson;
            return true;
        }

        return false;
    }

    private static bool IsValidJson(string s)
    {
        try
        {
            using var _ = JsonDocument.Parse(s);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryExtractBalancedJson(string text, char open, char close, out string json)
    {
        json = string.Empty;
        int start = 0;
        while (true)
        {
            int idx = text.IndexOf(open, start);
            if (idx < 0) return false;

            int end = FindBalancedEnd(text, idx, open, close);
            if (end > idx)
            {
                json = text.Substring(idx, end - idx + 1).Trim();
                return true;
            }

            start = idx + 1;
        }
    }

    private static int FindBalancedEnd(string text, int startIndex, char open, char close)
    {
        int depth = 0;
        bool inString = false;
        bool escape = false;

        for (int i = startIndex; i < text.Length; i++)
        {
            char c = text[i];

            if (inString)
            {
                if (escape)
                {
                    escape = false;
                }
                else if (c == '\\')
                {
                    escape = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }
                continue;
            }

            if (c == '"')
            {
                inString = true;
                continue;
            }

            if (c == open)
            {
                depth++;
            }
            else if (c == close)
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    // 内部类
    private class DiffChunk
    {
        public string FileName { get; set; } = "";
        public string Content { get; set; } = "";
        public int Size { get; set; }
    }

    private class ChunkReviewResult
    {
        public string FileName { get; set; } = "";
        public string ReviewResult { get; set; } = "";
        public int Order { get; set; }
        public bool HasError { get; set; }
    }

    /// <summary>
    /// 为分块请求增加重试与超时控制（简单指数退避）
    /// </summary>
    private async Task<string> ExecuteWithRetryAsync(
        Func<CancellationToken, Task<string>> action,
        int maxRetries,
        TimeSpan initialDelay,
        TimeSpan timeout,
        string processType,
        string fileName)
    {
        var attempt = 0;
        var delay = initialDelay;

        while (true)
        {
            attempt++;
            using var cts = new CancellationTokenSource(timeout);
            try
            {
                return await action(cts.Token);
            }
            catch (Exception ex)
            {
                if (attempt > maxRetries + 1)
                {
                    _logger.LogError(ex, "{ProcessType}分块重试耗尽: {FileName} (尝试{Attempt})", processType, fileName, attempt - 1);
                    throw;
                }

                // 对常见的可重试错误打印警告并延迟
                _logger.LogWarning(ex, "{ProcessType}分块失败: {FileName} (第{Attempt}次)，{DelayMs}ms后重试", processType, fileName, attempt, (int)delay.TotalMilliseconds);
                await Task.Delay(delay);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 4000));
            }
        }
    }
}
