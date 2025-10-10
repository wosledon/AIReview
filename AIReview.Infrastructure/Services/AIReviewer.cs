using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AIReview.Core.Interfaces;

namespace AIReview.Infrastructure.Services;

public class AIReviewer : IAIReviewer
{
    private readonly ILLMService _llmService;
    private readonly IContextBuilder _contextBuilder;
    private readonly ILogger<AIReviewer> _logger;

    public AIReviewer(ILLMService llmService, IContextBuilder contextBuilder, ILogger<AIReviewer> logger)
    {
        _llmService = llmService;
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

            // 生成评审提示
            var prompt = BuildReviewPrompt(diff, reviewContext);

            // 调用AI模型
            var reviewResponse = await _llmService.GenerateAsync(prompt);

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

    private string BuildReviewPrompt(string diff, ReviewContext context)
    {
        return $@"
请对以下代码变更进行详细评审，并以JSON格式返回结果：

**代码变更：**
```
{diff}
```

**项目上下文：**
- 编程语言：{context.Language}
- 项目类型：{context.ProjectType}
- 编码规范：{context.CodingStandards}

**评审要求：**
1. 代码质量和可读性
2. 潜在的bug和错误
3. 性能优化建议
4. 安全性检查
5. 编码规范遵循情况

**返回格式（严格按照以下JSON格式）：**
{{
  ""overallScore"": 85.5,
  ""summary"": ""代码整体质量良好，但有几个需要改进的地方..."",
  ""comments"": [
    {{
      ""filePath"": ""src/example.cs"",
      ""lineNumber"": 10,
      ""content"": ""建议使用更具描述性的变量名"",
      ""severity"": ""warning"",
      ""category"": ""style"",
      ""suggestion"": ""将变量名从'x'改为'userCount'""
    }}
  ],
  ""actionableItems"": [
    ""添加输入验证"",
    ""优化数据库查询""
  ]
}}

请确保：
- overallScore 是0-100之间的数字
- severity 只能是: ""info"", ""warning"", ""error""
- category 只能是: ""quality"", ""security"", ""performance"", ""style"", ""bug""
- 所有文本内容使用中文
- 返回纯JSON，不要包含任何其他文本
";
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
                OverallScore = root.GetProperty("overallScore").GetDouble(),
                Summary = root.GetProperty("summary").GetString() ?? "",
                Comments = new List<AIReviewComment>(),
                ActionableItems = new List<string>()
            };

            // 解析评论
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

            // 解析可执行项
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

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response as JSON, falling back to text parsing");
            return ParseReviewResponseFallback(response);
        }
    }

    private AIReviewResult ParseReviewResponseFallback(string response)
    {
        // 回退方案：使用正则表达式提取关键信息
        var result = new AIReviewResult
        {
            OverallScore = ExtractScore(response),
            Summary = ExtractSummary(response),
            Comments = ExtractComments(response),
            ActionableItems = ExtractActionableItems(response)
        };

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
}