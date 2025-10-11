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

    public AIReviewer(IMultiLLMService multiLLMService, IContextBuilder contextBuilder, ILogger<AIReviewer> logger)
    {
        _multiLLMService = multiLLMService;
        _contextBuilder = contextBuilder;
        _logger = logger;
    }

    public async Task<AIReviewResult> ReviewCodeAsync(string diff, ReviewContext context)
    {
        try
        {
            _logger.LogInformation("Starting AI code review for language: {Language}", context.Language);

            // 构建评审上下文
            var reviewContext = await _contextBuilder.BuildContextAsync(diff, context);

            // 使用MultiLLMService自动选择可用的LLM进行代码评审
            var reviewResponse = await _multiLLMService.GenerateReviewAsync(diff, FormatContextForLLM(reviewContext));

            // 解析评审结果
            var result = ParseReviewResponse(reviewResponse);

            _logger.LogInformation("AI code review completed. Score: {Score}, Comments: {CommentCount}", 
                result.OverallScore, result.Comments.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI code review failed");
            throw;
        }
    }

    private string FormatContextForLLM(ReviewContext context)
    {
        return $@"编程语言：{context.Language}
项目类型：{context.ProjectType}
编码规范：{context.CodingStandards}";
    }

    private AIReviewResult ParseReviewResponse(string response)
    {
        try
        {
            // 清理响应，移除可能的代码块标记
            var cleanResponse = response.Trim();
            if (cleanResponse.StartsWith("```json"))
            {
                cleanResponse = cleanResponse.Substring(7);
            }
            if (cleanResponse.EndsWith("```"))
            {
                cleanResponse = cleanResponse.Substring(0, cleanResponse.Length - 3);
            }
            cleanResponse = cleanResponse.Trim();

            // 解析JSON响应
            var jsonDoc = JsonDocument.Parse(cleanResponse);
            var root = jsonDoc.RootElement;

            var result = new AIReviewResult
            {
                Summary = root.GetProperty("summary").GetString() ?? "",
                Comments = new List<AIReviewComment>(),
                ActionableItems = new List<string>()
            };

            // 解析分数 - 支持多种格式
            if (root.TryGetProperty("overallScore", out var overallScoreElement))
            {
                result.OverallScore = overallScoreElement.GetDouble();
            }
            else if (root.TryGetProperty("score", out var scoreElement))
            {
                result.OverallScore = scoreElement.GetDouble();
            }
            else
            {
                result.OverallScore = 75.0; // 默认分数
            }

            // 解析评论 - 支持 comments 和 issues 两种格式
            if (root.TryGetProperty("comments", out var commentsElement))
            {
                foreach (var commentElement in commentsElement.EnumerateArray())
                {
                    var comment = new AIReviewComment
                    {
                        FilePath = commentElement.TryGetProperty("filePath", out var fp) ? fp.GetString() : null,
                        LineNumber = commentElement.TryGetProperty("lineNumber", out var ln) && ln.ValueKind == JsonValueKind.Number ? ln.GetInt32() : null,
                        Content = commentElement.GetProperty("content").GetString() ?? "",
                        Severity = commentElement.TryGetProperty("severity", out var sev) ? sev.GetString() ?? "info" : "info",
                        Category = commentElement.TryGetProperty("category", out var cat) ? cat.GetString() ?? "quality" : "quality",
                        Suggestion = commentElement.TryGetProperty("suggestion", out var sug) ? sug.GetString() : null
                    };
                    result.Comments.Add(comment);
                }
            }
            else if (root.TryGetProperty("issues", out var issuesElement))
            {
                foreach (var issueElement in issuesElement.EnumerateArray())
                {
                    var comment = new AIReviewComment
                    {
                        FilePath = issueElement.TryGetProperty("filePath", out var fp) ? fp.GetString() : null,
                        LineNumber = TryParseLineNumber(issueElement),
                        Content = issueElement.GetProperty("message").GetString() ?? "",
                        Severity = issueElement.TryGetProperty("severity", out var sev) ? sev.GetString() ?? "info" : "info",
                        Category = DetermineCategoryFromContent(issueElement.GetProperty("message").GetString() ?? ""),
                        Suggestion = issueElement.TryGetProperty("suggestion", out var sug) ? sug.GetString() : null
                    };
                    result.Comments.Add(comment);
                }
            }

            // 解析可执行项 - 支持 actionableItems 和 recommendations 两种格式
            if (root.TryGetProperty("actionableItems", out var itemsElement))
            {
                foreach (var itemElement in itemsElement.EnumerateArray())
                {
                    var item = itemElement.GetString();
                    if (!string.IsNullOrEmpty(item))
                    {
                        result.ActionableItems.Add(item);
                    }
                }
            }
            else if (root.TryGetProperty("recommendations", out var recommendationsElement))
            {
                foreach (var recommendationElement in recommendationsElement.EnumerateArray())
                {
                    var item = recommendationElement.GetString();
                    if (!string.IsNullOrEmpty(item))
                    {
                        result.ActionableItems.Add(item);
                    }
                }
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response as JSON, falling back to text parsing. Response: {Response}", 
                response.Length > 500 ? response.Substring(0, 500) + "..." : response);
            return ParseReviewResponseFallback(response);
        }
    }

    private AIReviewResult ParseReviewResponseFallback(string response)
    {
        _logger.LogInformation("Using fallback text parsing for AI response");
        
        // 回退方案：使用正则表达式提取关键信息
        var result = new AIReviewResult
        {
            OverallScore = ExtractScore(response),
            Summary = ExtractSummary(response),
            Comments = ExtractComments(response),
            ActionableItems = ExtractActionableItems(response)
        };

        _logger.LogDebug("Fallback parsing result: Score={Score}, Comments={CommentCount}, Items={ItemCount}",
            result.OverallScore, result.Comments.Count, result.ActionableItems.Count);

        return result;
    }

    private double ExtractScore(string response)
    {
        // 查找分数模式，如 "85分"、"评分：90"
        var scorePattern = @"(?:评分|分数|得分|score)[:：]\s*(\d+(?:\.\d+)?)";
        var match = Regex.Match(response, scorePattern, RegexOptions.IgnoreCase);
        
        if (match.Success && double.TryParse(match.Groups[1].Value, out var score))
        {
            return Math.Min(Math.Max(score, 0), 100); // 确保在0-100范围内
        }

        // 默认中等分数
        return 75.0;
    }

    private string ExtractSummary(string response)
    {
        // 提取总结部分
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var summaryLines = lines.Take(Math.Min(3, lines.Length)).ToList();
        
        return string.Join(" ", summaryLines).Trim();
    }

    private List<AIReviewComment> ExtractComments(string response)
    {
        var comments = new List<AIReviewComment>();
        
        // 简单的评论提取逻辑
        var lines = response.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("建议") || line.Contains("问题") || line.Contains("优化"))
            {
                comments.Add(new AIReviewComment
                {
                    Content = line.Trim(),
                    Severity = DetermineSeverity(line),
                    Category = DetermineCategory(line)
                });
            }
        }

        return comments;
    }

    private List<string> ExtractActionableItems(string response)
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
                items.Add(match.Groups[1].Value.Trim());
            }
        }

        return items;
    }

    private string DetermineSeverity(string text)
    {
        if (text.Contains("错误") || text.Contains("bug") || text.Contains("严重"))
            return "error";
        if (text.Contains("警告") || text.Contains("注意") || text.Contains("建议"))
            return "warning";
        return "info";
    }

    private string DetermineCategory(string text)
    {
        if (text.Contains("安全") || text.Contains("漏洞"))
            return "security";
        if (text.Contains("性能") || text.Contains("效率"))
            return "performance";
        if (text.Contains("风格") || text.Contains("格式") || text.Contains("命名"))
            return "style";
        if (text.Contains("bug") || text.Contains("错误"))
            return "bug";
        return "quality";
    }

    private int? TryParseLineNumber(JsonElement element)
    {
        if (element.TryGetProperty("lineNumber", out var lineNumberElement) && 
            lineNumberElement.ValueKind == JsonValueKind.Number)
        {
            return lineNumberElement.GetInt32();
        }
        
        if (element.TryGetProperty("line", out var lineElement))
        {
            if (lineElement.ValueKind == JsonValueKind.Number)
            {
                return lineElement.GetInt32();
            }
            if (lineElement.ValueKind == JsonValueKind.String && 
                int.TryParse(lineElement.GetString(), out var lineNumber))
            {
                return lineNumber;
            }
        }
        
        return null;
    }

    private string DetermineCategoryFromContent(string content)
    {
        var lowerContent = content.ToLowerInvariant();
        
        if (lowerContent.Contains("安全") || lowerContent.Contains("漏洞") || lowerContent.Contains("security"))
            return "security";
        if (lowerContent.Contains("性能") || lowerContent.Contains("效率") || lowerContent.Contains("performance"))
            return "performance";
        if (lowerContent.Contains("风格") || lowerContent.Contains("格式") || lowerContent.Contains("命名") || 
            lowerContent.Contains("style") || lowerContent.Contains("naming"))
            return "style";
        if (lowerContent.Contains("bug") || lowerContent.Contains("错误") || lowerContent.Contains("缺陷"))
            return "bug";
        if (lowerContent.Contains("重构") || lowerContent.Contains("设计") || lowerContent.Contains("架构"))
            return "design";
        
        return "quality";
    }
}