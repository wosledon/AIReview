using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AIReview.Core.Interfaces;

namespace AIReview.Infrastructure.Services;

public class AIReviewer : IAIReviewer
{
    private readonly IMultiLLMService _multiLLMService;
    private readonly IContextBuilder _contextBuilder;
    private readonly ILogger<AIReviewer> _logger;

    // 严重程度关键词映射
    private static readonly Dictionary<string, List<string>> SeverityKeywords = new()
    {
        ["high"] = new() { "critical", "严重", "错误", "bug", "漏洞", "崩溃", "安全" },
        ["medium"] = new() { "警告", "warning", "注意", "问题", "风险" },
        ["low"] = new() { "建议", "suggestion", "优化", "改进", "提示" }
    };

    // 类别关键词映射
    private static readonly Dictionary<string, List<string>> CategoryKeywords = new()
    {
        ["security"] = new() { "安全", "漏洞", "security", "vulnerability", "injection", "xss", "csrf" },
        ["performance"] = new() { "性能", "效率", "performance", "optimization", "slow", "memory", "cpu" },
        ["style"] = new() { "风格", "格式", "命名", "style", "naming", "formatting", "convention" },
        ["bug"] = new() { "bug", "错误", "缺陷", "error", "defect", "fault" },
        ["design"] = new() { "重构", "设计", "架构", "refactor", "design", "architecture", "pattern" },
        ["maintainability"] = new() { "可维护", "复杂", "耦合", "maintainability", "complexity", "coupling" }
    };

    public AIReviewer(IMultiLLMService multiLLMService, IContextBuilder contextBuilder, ILogger<AIReviewer> logger)
    {
        _multiLLMService = multiLLMService;
        _contextBuilder = contextBuilder;
        _logger = logger;
    }

    public async Task<AIReviewResult> ReviewCodeAsync(string diff, ReviewContext context)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            _logger.LogInformation("Starting AI code review for language: {Language}", context.Language);

            // 构建评审上下文
            var reviewContext = await _contextBuilder.BuildContextAsync(diff, context);

            // 使用 IMultiLLMService 的自动分块评审方法
            // 如果代码量超过LLM限制,会自动按文件分块评审并汇总结果
            var reviewResponse = await _multiLLMService.ReviewWithAutoChunkingAsync(
                diff, 
                FormatContextForLLM(reviewContext));

            // 解析评审结果
            var result = ParseReviewResponse(reviewResponse);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("AI code review completed. Score: {Score}, Comments: {CommentCount}, Duration: {DurationMs}ms", 
                result.OverallScore, result.Comments.Count, duration.TotalMilliseconds);

            return result;
        }
        catch (JsonException ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "AI code review failed due to JSON parsing error, Duration: {DurationMs}ms", duration.TotalMilliseconds);
            throw new InvalidOperationException("AI返回的结果格式无效,请检查LLM配置", ex);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "AI code review failed, Duration: {DurationMs}ms", duration.TotalMilliseconds);
            throw;
        }
    }

    private string FormatContextForLLM(ReviewContext context)
    {
        return $@"编程语言：{context.Language}
项目类型：{context.ProjectType}
编码规范：{context.CodingStandards}";
    }

    /// <summary>
    /// 解析AI返回的评审响应,支持多种JSON格式
    /// </summary>
    private AIReviewResult ParseReviewResponse(string response)
    {
        try
        {
            // 清理响应,移除可能的代码块标记和空白
            var cleanResponse = CleanJsonResponse(response);
            
            if (string.IsNullOrWhiteSpace(cleanResponse))
            {
                _logger.LogWarning("AI返回了空的响应,使用回退解析");
                return ParseReviewResponseFallback(response);
            }

            // 解析JSON响应
            var jsonDoc = JsonDocument.Parse(cleanResponse);
            var root = jsonDoc.RootElement;

            var result = new AIReviewResult
            {
                Summary = ExtractSummaryFromJson(root),
                Comments = ExtractCommentsFromJson(root),
                ActionableItems = ExtractActionableItemsFromJson(root),
                OverallScore = ExtractScoreFromJson(root)
            };

            _logger.LogDebug("Successfully parsed AI response: Score={Score}, Comments={CommentCount}, Items={ItemCount}",
                result.OverallScore, result.Comments.Count, result.ActionableItems.Count);

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response as JSON, falling back to text parsing. Response length: {Length}", 
                response.Length);
            return ParseReviewResponseFallback(response);
        }
    }

    /// <summary>
    /// 清理JSON响应,移除markdown代码块标记
    /// </summary>
    private string CleanJsonResponse(string response)
    {
        var cleaned = response.Trim();
        
        // 移除markdown代码块标记
        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(7);
        }
        else if (cleaned.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(3);
        }
        
        if (cleaned.EndsWith("```"))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 3);
        }
        
        return cleaned.Trim();
    }

    /// <summary>
    /// 从JSON中提取摘要信息
    /// </summary>
    private string ExtractSummaryFromJson(JsonElement root)
    {
        if (root.TryGetProperty("summary", out var summaryElement))
        {
            return summaryElement.GetString() ?? "无摘要信息";
        }
        
        // 尝试其他可能的字段名
        if (root.TryGetProperty("overview", out var overviewElement))
        {
            return overviewElement.GetString() ?? "无摘要信息";
        }
        
        return "无摘要信息";
    }

    /// <summary>
    /// 从JSON中提取评分
    /// </summary>
    private double ExtractScoreFromJson(JsonElement root)
    {
        // 尝试多种可能的评分字段
        var scoreFields = new[] { "overallScore", "score", "qualityScore", "rating" };
        
        foreach (var field in scoreFields)
        {
            if (root.TryGetProperty(field, out var scoreElement))
            {
                if (scoreElement.ValueKind == JsonValueKind.Number)
                {
                    var score = scoreElement.GetDouble();
                    return NormalizeScore(score);
                }
                else if (scoreElement.ValueKind == JsonValueKind.String && 
                         double.TryParse(scoreElement.GetString(), out var parsedScore))
                {
                    return NormalizeScore(parsedScore);
                }
            }
        }

        _logger.LogWarning("No score found in AI response, using default score 75");
        return 75.0; // 默认中等分数
    }

    /// <summary>
    /// 标准化评分到0-100范围
    /// </summary>
    private double NormalizeScore(double score)
    {
        // 如果分数在0-10范围,转换为0-100
        if (score >= 0 && score <= 10)
        {
            return score * 10;
        }
        
        // 确保在0-100范围内
        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// 从JSON中提取评论列表
    /// </summary>
    private List<AIReviewComment> ExtractCommentsFromJson(JsonElement root)
    {
        var comments = new List<AIReviewComment>();

        // 尝试 "comments" 字段
        if (root.TryGetProperty("comments", out var commentsElement))
        {
            ParseCommentArray(commentsElement, comments, isIssueFormat: false);
        }
        // 尝试 "issues" 字段
        else if (root.TryGetProperty("issues", out var issuesElement))
        {
            ParseCommentArray(issuesElement, comments, isIssueFormat: true);
        }
        // 尝试 "findings" 字段
        else if (root.TryGetProperty("findings", out var findingsElement))
        {
            ParseCommentArray(findingsElement, comments, isIssueFormat: false);
        }

        _logger.LogDebug("Extracted {CommentCount} comments from JSON", comments.Count);
        return comments;
    }

    /// <summary>
    /// 解析评论数组
    /// </summary>
    private void ParseCommentArray(JsonElement arrayElement, List<AIReviewComment> comments, bool isIssueFormat)
    {
        if (arrayElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var element in arrayElement.EnumerateArray())
        {
            try
            {
                var comment = ParseSingleComment(element, isIssueFormat);
                if (comment != null)
                {
                    comments.Add(comment);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse single comment, skipping");
            }
        }
    }

    /// <summary>
    /// 解析单个评论对象
    /// </summary>
    private AIReviewComment? ParseSingleComment(JsonElement element, bool isIssueFormat)
    {
        // 提取内容/消息
        var content = ExtractCommentContent(element, isIssueFormat);
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var comment = new AIReviewComment
        {
            FilePath = ExtractFilePath(element),
            LineNumber = ExtractLineNumber(element),
            Content = content,
            Severity = ExtractSeverity(element, content),
            Category = ExtractCategory(element, content),
            Suggestion = ExtractSuggestion(element)
        };

        return comment;
    }

    private string ExtractCommentContent(JsonElement element, bool isIssueFormat)
    {
        var contentFields = isIssueFormat 
            ? new[] { "message", "description", "content" }
            : new[] { "content", "message", "description" };

        foreach (var field in contentFields)
        {
            if (element.TryGetProperty(field, out var contentElement) && 
                contentElement.ValueKind == JsonValueKind.String)
            {
                return contentElement.GetString() ?? "";
            }
        }

        return "";
    }

    private string? ExtractFilePath(JsonElement element)
    {
        var fileFields = new[] { "filePath", "file", "path", "fileName" };
        
        foreach (var field in fileFields)
        {
            if (element.TryGetProperty(field, out var pathElement) && 
                pathElement.ValueKind == JsonValueKind.String)
            {
                return pathElement.GetString();
            }
        }

        return null;
    }

    private int? ExtractLineNumber(JsonElement element)
    {
        var lineFields = new[] { "lineNumber", "line", "lineNo", "startLine" };
        
        foreach (var field in lineFields)
        {
            if (element.TryGetProperty(field, out var lineElement))
            {
                if (lineElement.ValueKind == JsonValueKind.Number)
                {
                    return lineElement.GetInt32();
                }
                else if (lineElement.ValueKind == JsonValueKind.String && 
                         int.TryParse(lineElement.GetString(), out var lineNumber))
                {
                    return lineNumber;
                }
            }
        }

        return null;
    }

    private string ExtractSeverity(JsonElement element, string content)
    {
        // 首先尝试从JSON字段提取
        if (element.TryGetProperty("severity", out var severityElement) && 
            severityElement.ValueKind == JsonValueKind.String)
        {
            var severity = severityElement.GetString()?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(severity))
            {
                return NormalizeSeverity(severity);
            }
        }

        // 如果JSON中没有,从内容推断
        return InferSeverityFromContent(content);
    }

    private string NormalizeSeverity(string severity)
    {
        severity = severity.ToLowerInvariant();
        
        return severity switch
        {
            "critical" or "error" or "high" or "严重" => "high",
            "warning" or "medium" or "中等" or "警告" => "medium",
            "info" or "low" or "suggestion" or "低" or "建议" => "low",
            _ => "medium"
        };
    }

    private string InferSeverityFromContent(string content)
    {
        var lowerContent = content.ToLowerInvariant();

        foreach (var (severity, keywords) in SeverityKeywords)
        {
            if (keywords.Any(kw => lowerContent.Contains(kw)))
            {
                return severity;
            }
        }

        return "medium"; // 默认中等严重程度
    }

    private string ExtractCategory(JsonElement element, string content)
    {
        // 首先尝试从JSON字段提取
        if (element.TryGetProperty("category", out var categoryElement) && 
            categoryElement.ValueKind == JsonValueKind.String)
        {
            var category = categoryElement.GetString();
            if (!string.IsNullOrEmpty(category))
            {
                return NormalizeCategory(category);
            }
        }

        // 如果JSON中没有,从内容推断
        return InferCategoryFromContent(content);
    }

    private string NormalizeCategory(string category)
    {
        category = category.ToLowerInvariant();
        
        var categoryMap = new Dictionary<string, string>
        {
            ["security"] = "security",
            ["安全"] = "security",
            ["performance"] = "performance",
            ["性能"] = "performance",
            ["style"] = "style",
            ["风格"] = "style",
            ["bug"] = "bug",
            ["缺陷"] = "bug",
            ["design"] = "design",
            ["设计"] = "design",
            ["maintainability"] = "maintainability",
            ["可维护性"] = "maintainability"
        };

        return categoryMap.TryGetValue(category, out var normalized) ? normalized : "quality";
    }

    private string InferCategoryFromContent(string content)
    {
        var lowerContent = content.ToLowerInvariant();

        foreach (var (category, keywords) in CategoryKeywords)
        {
            if (keywords.Any(kw => lowerContent.Contains(kw)))
            {
                return category;
            }
        }

        return "quality"; // 默认代码质量类别
    }

    private string? ExtractSuggestion(JsonElement element)
    {
        var suggestionFields = new[] { "suggestion", "fix", "recommendation", "solution" };
        
        foreach (var field in suggestionFields)
        {
            if (element.TryGetProperty(field, out var suggestionElement) && 
                suggestionElement.ValueKind == JsonValueKind.String)
            {
                return suggestionElement.GetString();
            }
        }

        return null;
    }

    /// <summary>
    /// 从JSON中提取可执行项列表
    /// </summary>
    private List<string> ExtractActionableItemsFromJson(JsonElement root)
    {
        var items = new List<string>();

        var itemFields = new[] { "actionableItems", "recommendations", "suggestions", "actions" };

        foreach (var field in itemFields)
        {
            if (root.TryGetProperty(field, out var itemsElement) && 
                itemsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var itemElement in itemsElement.EnumerateArray())
                {
                    if (itemElement.ValueKind == JsonValueKind.String)
                    {
                        var item = itemElement.GetString();
                        if (!string.IsNullOrWhiteSpace(item))
                        {
                            items.Add(item);
                        }
                    }
                }
                
                if (items.Count > 0)
                {
                    break; // 找到后就退出
                }
            }
        }

        return items;
    }

    /// <summary>
    /// 回退方案:使用文本解析提取评审信息
    /// </summary>
    private AIReviewResult ParseReviewResponseFallback(string response)
    {
        _logger.LogInformation("Using fallback text parsing for AI response");
        
        var result = new AIReviewResult
        {
            OverallScore = ExtractScoreFromText(response),
            Summary = ExtractSummaryFromText(response),
            Comments = ExtractCommentsFromText(response),
            ActionableItems = ExtractActionableItemsFromText(response)
        };

        _logger.LogDebug("Fallback parsing result: Score={Score}, Comments={CommentCount}, Items={ItemCount}",
            result.OverallScore, result.Comments.Count, result.ActionableItems.Count);

        return result;
    }

    private double ExtractScoreFromText(string response)
    {
        // 多种分数模式匹配
        var scorePatterns = new[]
        {
            @"(?:评分|分数|得分|score)[:：]\s*(\d+(?:\.\d+)?)",
            @"(\d+(?:\.\d+)?)\s*[/分]?\s*(?:out of 100|满分)",
            @"quality\s*[:：]\s*(\d+(?:\.\d+)?)"
        };

        foreach (var pattern in scorePatterns)
        {
            var match = Regex.Match(response, pattern, RegexOptions.IgnoreCase);
            if (match.Success && double.TryParse(match.Groups[1].Value, out var score))
            {
                return NormalizeScore(score);
            }
        }

        _logger.LogWarning("Could not extract score from text, using default value 75");
        return 75.0;
    }

    private string ExtractSummaryFromText(string response)
    {
        // 提取总结部分
        var summaryPatterns = new[]
        {
            @"(?:总结|摘要|summary)[:：]\s*(.+?)(?:\n\n|\n(?:问题|建议|评分))",
            @"^(.+?)(?:\n\n|\n(?:问题|建议|评分))"
        };

        foreach (var pattern in summaryPatterns)
        {
            var match = Regex.Match(response, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success)
            {
                var summary = match.Groups[1].Value.Trim();
                if (summary.Length > 20) // 确保有实质内容
                {
                    // 限制长度
                    return summary.Length > 500 ? summary.Substring(0, 500) + "..." : summary;
                }
            }
        }

        // 如果没有匹配到,返回前几行
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var summaryLines = lines.Take(Math.Min(3, lines.Length)).ToList();
        var fallbackSummary = string.Join(" ", summaryLines).Trim();
        
        return fallbackSummary.Length > 200 
            ? fallbackSummary.Substring(0, 200) + "..." 
            : fallbackSummary;
    }

    private List<AIReviewComment> ExtractCommentsFromText(string response)
    {
        var comments = new List<AIReviewComment>();
        
        // 查找问题描述模式
        var problemPatterns = new[]
        {
            @"(?:问题|issue|problem|建议|suggestion)[:：]\s*(.+?)(?=\n(?:问题|issue|建议|评分|$))",
            @"^\s*[\d\-\*•]\s*(.+?)$"
        };

        var lines = response.Split('\n');
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.Length < 10)
            {
                continue;
            }

            // 检查是否包含关键提示词
            if (ContainsReviewKeywords(trimmedLine))
            {
                var comment = new AIReviewComment
                {
                    Content = trimmedLine,
                    Severity = InferSeverityFromContent(trimmedLine),
                    Category = InferCategoryFromContent(trimmedLine),
                    FilePath = ExtractFilePathFromText(trimmedLine),
                    LineNumber = ExtractLineNumberFromText(trimmedLine)
                };
                comments.Add(comment);
            }
        }

        // 如果没有找到任何评论,创建一个通用评论
        if (comments.Count == 0)
        {
            comments.Add(new AIReviewComment
            {
                Content = "代码审查已完成,未发现明显问题",
                Severity = "low",
                Category = "quality"
            });
        }

        return comments;
    }

    private bool ContainsReviewKeywords(string text)
    {
        var keywords = new[] 
        { 
            "建议", "问题", "错误", "警告", "优化", "改进", "注意",
            "bug", "issue", "warning", "error", "suggestion", "recommend",
            "应该", "需要", "可以", "避免", "使用", "考虑"
        };

        var lowerText = text.ToLowerInvariant();
        return keywords.Any(kw => lowerText.Contains(kw));
    }

    private string? ExtractFilePathFromText(string text)
    {
        // 查找文件路径模式
        var pathPattern = @"(?:文件|file|path)[:：]?\s*([^\s]+\.[a-z]+)";
        var match = Regex.Match(text, pathPattern, RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        // 尝试直接匹配文件路径格式
        var directPathPattern = @"([a-zA-Z]:\\[^\s]+|/[^\s]+|\.?/?[a-zA-Z0-9_\-]+/[^\s]+\.[a-z]{2,4})";
        match = Regex.Match(text, directPathPattern);
        
        return match.Success ? match.Groups[1].Value : null;
    }

    private int? ExtractLineNumberFromText(string text)
    {
        // 查找行号模式
        var linePatterns = new[]
        {
            @"(?:行|line|L)[:：]?\s*(\d+)",
            @"@\s*(\d+)",
            @"#(\d+)"
        };

        foreach (var pattern in linePatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var lineNumber))
            {
                return lineNumber;
            }
        }

        return null;
    }

    private List<string> ExtractActionableItemsFromText(string response)
    {
        var items = new List<string>();
        
        // 查找以数字或项目符号开头的行
        var itemPattern = @"^\s*[\d\-\*•]\s*(.+)$";
        var lines = response.Split('\n');
        
        foreach (var line in lines)
        {
            var match = Regex.Match(line, itemPattern);
            if (match.Success)
            {
                var item = match.Groups[1].Value.Trim();
                // 过滤掉太短或不像建议的内容
                if (item.Length >= 10 && ContainsReviewKeywords(item))
                {
                    items.Add(item);
                }
            }
        }

        // 如果没有找到,尝试提取包含关键动词的句子
        if (items.Count == 0)
        {
            var actionVerbs = new[] { "建议", "应该", "需要", "考虑", "推荐", "recommend", "should", "need", "consider" };
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (actionVerbs.Any(verb => trimmedLine.Contains(verb, StringComparison.OrdinalIgnoreCase)) &&
                    trimmedLine.Length >= 15)
                {
                    items.Add(trimmedLine);
                }
            }
        }

        return items;
    }
}