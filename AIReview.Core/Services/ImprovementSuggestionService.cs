using Microsoft.Extensions.Logging;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;
using AIReview.Shared.Enums;
using System.Text.Json;

namespace AIReview.Core.Services;

public class ImprovementSuggestionService : IImprovementSuggestionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGitService _gitService;
    private readonly IDiffParserService _diffParserService;
    private readonly IMultiLLMService _llmService;
    private readonly ILogger<ImprovementSuggestionService> _logger;

    public ImprovementSuggestionService(
        IUnitOfWork unitOfWork,
        IGitService gitService,
        IDiffParserService diffParserService,
        IMultiLLMService llmService,
        ILogger<ImprovementSuggestionService> logger)
    {
        _unitOfWork = unitOfWork;
        _gitService = gitService;
        _diffParserService = diffParserService;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<List<ImprovementSuggestionDto>> GenerateImprovementSuggestionsAsync(int reviewRequestId)
    {
        var reviewRequest = await _unitOfWork.ReviewRequests.GetReviewWithProjectAsync(reviewRequestId);
        if (reviewRequest == null)
            throw new ArgumentException($"Review request with id {reviewRequestId} not found");

        if (reviewRequest.Project == null)
            throw new InvalidOperationException($"Project not found for review request {reviewRequestId}");

        _logger.LogInformation("Generating improvement suggestions for review {ReviewId}", reviewRequestId);

        try
        {
            // 获取项目关联的Git仓库
            var repositories = await _gitService.GetRepositoriesAsync(reviewRequest.ProjectId);
            var repository = repositories.FirstOrDefault();
            
            if (repository == null)
            {
                _logger.LogWarning("No Git repository found for project {ProjectId}", reviewRequest.ProjectId);
                throw new InvalidOperationException($"No Git repository found for project {reviewRequest.ProjectId}");
            }

            // 获取代码差异
            var diff = await _gitService.GetDiffBetweenRefsAsync(repository.Id, reviewRequest.BaseBranch, reviewRequest.Branch);
            
            if (string.IsNullOrEmpty(diff))
            {
                _logger.LogWarning("No diff found between {BaseBranch} and {Branch}", reviewRequest.BaseBranch, reviewRequest.Branch);
                return new List<ImprovementSuggestionDto>();
            }

            // 解析差异
            var parsedDiff = _diffParserService.ParseGitDiff(diff);
            var fileDiffs = ConvertToFileDiffs(parsedDiff);

            // 删除现有的改进建议
            await _unitOfWork.ImprovementSuggestions.DeleteByReviewRequestIdAsync(reviewRequestId);

            var suggestions = new List<ImprovementSuggestion>();

            // 为每个文件生成建议
            foreach (var fileDiff in fileDiffs)
            {
                var fileSuggestions = await GenerateFileSuggestionsAsync(reviewRequestId, fileDiff, diff);
                suggestions.AddRange(fileSuggestions);
            }

            // 生成整体建议
            var overallSuggestions = await GenerateOverallSuggestionsAsync(reviewRequestId, fileDiffs, diff);
            suggestions.AddRange(overallSuggestions);

            // 保存建议到数据库
            foreach (var suggestion in suggestions)
            {
                await _unitOfWork.ImprovementSuggestions.AddAsync(suggestion);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Generated {Count} improvement suggestions for review {ReviewId}", 
                suggestions.Count, reviewRequestId);

            return suggestions.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate improvement suggestions for review {ReviewId}", reviewRequestId);
            throw;
        }
    }

    public async Task<List<ImprovementSuggestionDto>> GetImprovementSuggestionsAsync(int reviewRequestId)
    {
        var suggestions = await _unitOfWork.ImprovementSuggestions.GetByReviewRequestIdAsync(reviewRequestId);
        return suggestions.Select(MapToDto).ToList();
    }

    public async Task<ImprovementSuggestionDto?> GetImprovementSuggestionAsync(int suggestionId)
    {
        var suggestion = await _unitOfWork.ImprovementSuggestions.GetByIdAsync(suggestionId);
        return suggestion != null ? MapToDto(suggestion) : null;
    }

    public async Task<ImprovementSuggestionDto> UpdateImprovementSuggestionAsync(int suggestionId, UpdateImprovementSuggestionRequest request)
    {
        var suggestion = await _unitOfWork.ImprovementSuggestions.GetByIdAsync(suggestionId);
        if (suggestion == null)
            throw new ArgumentException($"Improvement suggestion with id {suggestionId} not found");

        if (request.IsAccepted.HasValue)
            suggestion.IsAccepted = request.IsAccepted.Value;

        if (request.IsIgnored.HasValue)
            suggestion.IsIgnored = request.IsIgnored.Value;

        if (!string.IsNullOrEmpty(request.UserFeedback))
            suggestion.UserFeedback = request.UserFeedback;

        suggestion.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.ImprovementSuggestions.Update(suggestion);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated improvement suggestion {SuggestionId} status", suggestionId);

        return MapToDto(suggestion);
    }

    public async Task<List<ImprovementSuggestionDto>> BulkUpdateImprovementSuggestionsAsync(List<int> suggestionIds, UpdateImprovementSuggestionRequest request)
    {
        var suggestions = await _unitOfWork.ImprovementSuggestions.GetByIdsAsync(suggestionIds);
        
        foreach (var suggestion in suggestions)
        {
            if (request.IsAccepted.HasValue)
                suggestion.IsAccepted = request.IsAccepted.Value;

            if (request.IsIgnored.HasValue)
                suggestion.IsIgnored = request.IsIgnored.Value;

            if (!string.IsNullOrEmpty(request.UserFeedback))
                suggestion.UserFeedback = request.UserFeedback;

            suggestion.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.ImprovementSuggestions.Update(suggestion);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Bulk updated {Count} improvement suggestions", suggestions.Count);

        return suggestions.Select(MapToDto).ToList();
    }

    private async Task<List<ImprovementSuggestion>> GenerateFileSuggestionsAsync(int reviewRequestId, FileDiffDto fileDiff, string fullDiff)
    {
        var prompt = BuildFileSuggestionPrompt(fileDiff, fullDiff);
        
        try
        {
            var codeToAnalyze = string.Join("\n", fileDiff.AddedContent.Concat(fileDiff.DeletedContent));
            // 使用自动分块分析,当代码量超过限制时会自动分块处理
            var response = await _llmService.AnalyzeWithAutoChunkingAsync(prompt, codeToAnalyze);
            var aiSuggestions = ParseFileSuggestions(response);
            
            return aiSuggestions.Select(ai => new ImprovementSuggestion
            {
                ReviewRequestId = reviewRequestId,
                FilePath = fileDiff.FilePath,
                StartLine = ai.StartLine,
                EndLine = ai.EndLine,
                Type = ai.Type,
                Priority = ai.Priority,
                Title = ai.Title,
                Description = ai.Description,
                OriginalCode = ai.OriginalCode,
                SuggestedCode = ai.SuggestedCode,
                Reasoning = ai.Reasoning,
                ExpectedBenefits = ai.ExpectedBenefits,
                ImplementationComplexity = ai.ImplementationComplexity,
                ImpactAssessment = ai.ImpactAssessment,
                AIModelVersion = "gpt-4", // 应该从LLM服务获取
                ConfidenceScore = ai.ConfidenceScore,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI suggestions for file {FilePath}", fileDiff.FilePath);
            return GetFallbackSuggestions(reviewRequestId, fileDiff);
        }
    }

    private async Task<List<ImprovementSuggestion>> GenerateOverallSuggestionsAsync(int reviewRequestId, List<FileDiffDto> parsedDiff, string fullDiff)
    {
        var prompt = BuildOverallSuggestionPrompt(parsedDiff, fullDiff);
        
        try
        {
            // 使用自动分块分析,当代码量超过限制时会自动分块处理
            var response = await _llmService.AnalyzeWithAutoChunkingAsync(prompt, fullDiff);
            var aiSuggestions = ParseOverallSuggestions(response);
            
            return aiSuggestions.Select(ai => new ImprovementSuggestion
            {
                ReviewRequestId = reviewRequestId,
                Type = ai.Type,
                Priority = ai.Priority,
                Title = ai.Title,
                Description = ai.Description,
                Reasoning = ai.Reasoning,
                ExpectedBenefits = ai.ExpectedBenefits,
                ImplementationComplexity = ai.ImplementationComplexity,
                ImpactAssessment = ai.ImpactAssessment,
                AIModelVersion = "gpt-4",
                ConfidenceScore = ai.ConfidenceScore,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate overall AI suggestions");
            return new List<ImprovementSuggestion>();
        }
    }

    private string BuildFileSuggestionPrompt(FileDiffDto fileDiff, string fullDiff)
    {
        return $@"分析以下文件的代码变更，并提供具体的改进建议。

文件路径: {fileDiff.FilePath}
变更行数: +{fileDiff.AddedLines}/-{fileDiff.DeletedLines}

代码差异片段:
{GetFileDiffSection(fullDiff, fileDiff.FilePath)}

请提供改进建议 (JSON数组格式):
[
  {{
    ""startLine"": 行号 (可选),
    ""endLine"": 行号 (可选),
    ""type"": ""CodeQuality|Performance|Security|Maintainability|Testing|Documentation|Architecture|BestPractices|Refactoring|ErrorHandling"",
    ""priority"": ""Low|Medium|High|Critical"",
    ""title"": ""简短的建议标题"",
    ""description"": ""详细的建议描述"",
    ""originalCode"": ""原始代码片段 (如果适用)"",
    ""suggestedCode"": ""建议的改进代码 (如果适用)"",
    ""reasoning"": ""改进的理由"",
    ""expectedBenefits"": ""预期收益"",
    ""implementationComplexity"": 1-10,
    ""impactAssessment"": ""影响范围评估"",
    ""confidenceScore"": 0.0-1.0
  }}
]

重点关注:
- 代码质量和可读性
- 性能优化机会
- 安全漏洞
- 最佳实践遵循
- 测试覆盖";
    }

    private string BuildOverallSuggestionPrompt(List<FileDiffDto> parsedDiff, string fullDiff)
    {
        var filesSummary = string.Join("\n", parsedDiff.Select(f => 
            $"- {f.FilePath}: +{f.AddedLines}/-{f.DeletedLines} lines"));

        return $@"从整体架构和设计角度分析以下代码变更，提供宏观改进建议。

变更摘要:
{filesSummary}

请提供整体架构建议 (JSON数组格式):
[
  {{
    ""type"": ""Architecture|BestPractices|Performance|Security|Testing|Documentation"",
    ""priority"": ""Low|Medium|High|Critical"",
    ""title"": ""建议标题"",
    ""description"": ""详细描述"",
    ""reasoning"": ""建议理由"",
    ""expectedBenefits"": ""预期收益"",
    ""implementationComplexity"": 1-10,
    ""impactAssessment"": ""影响评估"",
    ""confidenceScore"": 0.0-1.0
  }}
]

重点关注:
- 架构设计模式
- 系统可扩展性
- 模块化和解耦
- 性能瓶颈
- 安全架构";
    }

    private List<AISuggestion> ParseFileSuggestions(string response)
    {
        try
        {
            // 预处理：清理围栏、BOM，并从文本中提取首个完整 JSON 片段
            var cleanResponse = CleanJsonResponse(response);

            string? jsonPayload = ExtractFirstJsonPayload(cleanResponse);
            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                _logger.LogWarning("AI suggestions response does not contain JSON payload. First 200 chars: {Snippet}",
                    TruncateForLog(cleanResponse, 200));
                return new List<AISuggestion>();
            }

            // 支持数组或对象包裹（包含 suggestions 字段）
            List<JsonElement> items;
            try
            {
                var asArray = JsonSerializer.Deserialize<JsonElement[]>(jsonPayload);
                if (asArray != null)
                {
                    items = asArray.ToList();
                }
                else
                {
                    items = new List<JsonElement>();
                }
            }
            catch
            {
                // 尝试对象形式
                var obj = JsonSerializer.Deserialize<JsonElement>(jsonPayload);
                if (obj.ValueKind == JsonValueKind.Object && TryGetPropertyCaseInsensitive(obj, "suggestions", out var suggestionsEl) && suggestionsEl.ValueKind == JsonValueKind.Array)
                {
                    items = suggestionsEl.EnumerateArray().ToList();
                }
                else if (obj.ValueKind == JsonValueKind.Array)
                {
                    items = obj.EnumerateArray().ToList();
                }
                else
                {
                    items = new List<JsonElement>();
                }
            }

            var parsed = new List<AISuggestion>();
            foreach (var json in items)
            {
                // 宽松解析，属性名大小写不敏感，缺失则给默认
                TryGetPropertyCaseInsensitive(json, "startLine", out var startLineEl);
                TryGetPropertyCaseInsensitive(json, "endLine", out var endLineEl);
                TryGetPropertyCaseInsensitive(json, "type", out var typeEl);
                TryGetPropertyCaseInsensitive(json, "priority", out var priorityEl);
                TryGetPropertyCaseInsensitive(json, "title", out var titleEl);
                TryGetPropertyCaseInsensitive(json, "description", out var descriptionEl);
                TryGetPropertyCaseInsensitive(json, "originalCode", out var originalEl);
                TryGetPropertyCaseInsensitive(json, "suggestedCode", out var suggestedEl);
                TryGetPropertyCaseInsensitive(json, "reasoning", out var reasoningEl);
                TryGetPropertyCaseInsensitive(json, "expectedBenefits", out var benefitsEl);
                TryGetPropertyCaseInsensitive(json, "implementationComplexity", out var complexityEl);
                TryGetPropertyCaseInsensitive(json, "impactAssessment", out var impactEl);
                TryGetPropertyCaseInsensitive(json, "confidenceScore", out var confidenceEl);

                var item = new AISuggestion
                {
                    StartLine = GetIntOrNull(startLineEl),
                    EndLine = GetIntOrNull(endLineEl),
                    Type = GetEnumOrDefault(typeEl, ImprovementType.BestPractices),
                    Priority = GetEnumOrDefault(priorityEl, ImprovementPriority.Medium),
                    Title = GetStringOrDefault(titleEl, "AI 改进建议"),
                    Description = GetStringOrDefault(descriptionEl, "模型未提供详细描述。"),
                    OriginalCode = GetOptionalString(originalEl),
                    SuggestedCode = GetOptionalString(suggestedEl),
                    Reasoning = GetOptionalString(reasoningEl),
                    ExpectedBenefits = GetOptionalString(benefitsEl),
                    ImplementationComplexity = ClampToRange(GetIntOrDefault(complexityEl, 3), 1, 10),
                    ImpactAssessment = GetOptionalString(impactEl),
                    ConfidenceScore = ClampToRange(GetDoubleOrDefault(confidenceEl, 0.5), 0, 1)
                };
                parsed.Add(item);
            }

            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI suggestions response. Raw snippet: {Snippet}", TruncateForLog(response, 200));
            return new List<AISuggestion>();
        }
    }

    private List<AISuggestion> ParseOverallSuggestions(string response)
    {
        // 与ParseFileSuggestions类似，但不包含行号信息
        return ParseFileSuggestions(response);
    }

    private string GetFileDiffSection(string fullDiff, string filePath)
    {
        // 从完整差异中提取特定文件的差异片段
        var lines = fullDiff.Split('\n');
        var fileStart = -1;
        var fileEnd = -1;
        
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("diff --git") && lines[i].Contains(filePath))
            {
                fileStart = i;
            }
            else if (fileStart != -1 && lines[i].StartsWith("diff --git") && !lines[i].Contains(filePath))
            {
                fileEnd = i;
                break;
            }
        }
        
        if (fileStart == -1) return "";
        if (fileEnd == -1) fileEnd = lines.Length;
        
        return string.Join("\n", lines.Skip(fileStart).Take(fileEnd - fileStart));
    }

    private List<ImprovementSuggestion> GetFallbackSuggestions(int reviewRequestId, FileDiffDto fileDiff)
    {
        // 基于文件类型和变更提供基础建议
        var suggestions = new List<ImprovementSuggestion>();
        
        if (fileDiff.FilePath.EndsWith(".cs"))
        {
            suggestions.Add(new ImprovementSuggestion
            {
                ReviewRequestId = reviewRequestId,
                FilePath = fileDiff.FilePath,
                Type = ImprovementType.CodeQuality,
                Priority = ImprovementPriority.Medium,
                Title = "代码质量检查建议",
                Description = "建议进行代码质量检查，确保遵循编码规范",
                ImplementationComplexity = 3,
                AIModelVersion = "fallback",
                ConfidenceScore = 0.3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        
        return suggestions;
    }

    private ImprovementSuggestionDto MapToDto(ImprovementSuggestion entity)
    {
        return new ImprovementSuggestionDto
        {
            Id = entity.Id,
            ReviewRequestId = entity.ReviewRequestId,
            FilePath = entity.FilePath,
            StartLine = entity.StartLine,
            EndLine = entity.EndLine,
            Type = entity.Type,
            Priority = entity.Priority,
            Title = entity.Title,
            Description = entity.Description,
            OriginalCode = entity.OriginalCode,
            SuggestedCode = entity.SuggestedCode,
            Reasoning = entity.Reasoning,
            ExpectedBenefits = entity.ExpectedBenefits,
            ImplementationComplexity = entity.ImplementationComplexity,
            ImpactAssessment = entity.ImpactAssessment,
            IsAccepted = entity.IsAccepted,
            IsIgnored = entity.IsIgnored,
            UserFeedback = entity.UserFeedback,
            AIModelVersion = entity.AIModelVersion,
            ConfidenceScore = entity.ConfidenceScore,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private string CleanJsonResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return response;

        // 移除 UTF-8 BOM（如存在）
        var trimmed = response.TrimStart('\uFEFF').Trim();

        // 处理 ```json ... ``` 格式
        if (trimmed.StartsWith("```json"))
        {
            var start = trimmed.IndexOf("```json") + 7;
            var end = trimmed.LastIndexOf("```");
            if (end > start)
            {
                return trimmed.Substring(start, end - start).Trim();
            }
        }
        // 处理 ``` ... ``` 格式
        else if (trimmed.StartsWith("```"))
        {
            var start = trimmed.IndexOf("```") + 3;
            var end = trimmed.LastIndexOf("```");
            if (end > start)
            {
                return trimmed.Substring(start, end - start).Trim();
            }
        }

        return trimmed;
    }

    // 从文本中提取第一个完整的 JSON 片段（数组或对象），用于处理中文前缀/后缀包裹
    private string? ExtractFirstJsonPayload(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        int idxArray = text.IndexOf('[');
        int idxObj = text.IndexOf('{');
        int start = -1;
        char open = '\0';
        char close = '\0';

        if (idxArray >= 0 && (idxObj < 0 || idxArray < idxObj))
        {
            start = idxArray; open = '['; close = ']';
        }
        else if (idxObj >= 0)
        {
            start = idxObj; open = '{'; close = '}';
        }

        if (start < 0) return null;

        int depth = 0;
        bool inString = false;
        bool escape = false;
        for (int i = start; i < text.Length; i++)
        {
            char c = text[i];
            if (escape)
            {
                escape = false; continue;
            }
            if (c == '\\') { if (inString) escape = true; continue; }
            if (c == '"') { inString = !inString; continue; }

            if (!inString)
            {
                if (c == open) depth++;
                else if (c == close)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return text.Substring(start, i - start + 1);
                    }
                }
            }
        }
        // 未找到闭合，返回从起始到末尾（容错）
        return text.Substring(start);
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default; return false;
        }
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value; return true;
            }
        }
        value = default; return false;
    }

    private static string GetStringOrDefault(JsonElement el, string defaultValue)
    {
        try
        {
            return el.ValueKind == JsonValueKind.String ? el.GetString() ?? defaultValue : defaultValue;
        }
        catch { return defaultValue; }
    }

    private static string? GetOptionalString(JsonElement el)
    {
        try
        {
            return el.ValueKind == JsonValueKind.String ? el.GetString() : null;
        }
        catch { return null; }
    }

    private static int? GetIntOrNull(JsonElement el)
    {
        try
        {
            if (el.ValueKind == JsonValueKind.Number) return el.GetInt32();
            if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var v)) return v;
            return null;
        }
        catch { return null; }
    }

    private static int GetIntOrDefault(JsonElement el, int defaultValue)
    {
        var v = GetIntOrNull(el);
        return v ?? defaultValue;
    }

    private static double GetDoubleOrDefault(JsonElement el, double defaultValue)
    {
        try
        {
            if (el.ValueKind == JsonValueKind.Number) return el.GetDouble();
            if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), out var v)) return v;
            return defaultValue;
        }
        catch { return defaultValue; }
    }

    private static TEnum GetEnumOrDefault<TEnum>(JsonElement el, TEnum defaultValue) where TEnum : struct
    {
        try
        {
            if (el.ValueKind == JsonValueKind.String)
            {
                var s = el.GetString();
                if (!string.IsNullOrWhiteSpace(s) && Enum.TryParse<TEnum>(s, ignoreCase: true, out var parsed))
                {
                    return parsed;
                }
            }
            else if (el.ValueKind == JsonValueKind.Number)
            {
                // 支持用数字枚举值
                var i = el.GetInt32();
                if (Enum.IsDefined(typeof(TEnum), i))
                {
                    return (TEnum)Enum.ToObject(typeof(TEnum), i);
                }
            }
        }
        catch { }
        return defaultValue;
    }

    private static int ClampToRange(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static double ClampToRange(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static string TruncateForLog(string text, int maxLen)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Length <= maxLen ? text : text.Substring(0, maxLen);
    }

    private class AISuggestion
    {
        public int? StartLine { get; set; }
        public int? EndLine { get; set; }
        public ImprovementType Type { get; set; }
        public ImprovementPriority Priority { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? OriginalCode { get; set; }
        public string? SuggestedCode { get; set; }
        public string? Reasoning { get; set; }
        public string? ExpectedBenefits { get; set; }
        public int ImplementationComplexity { get; set; }
        public string? ImpactAssessment { get; set; }
        public double ConfidenceScore { get; set; }
    }

    private List<FileDiffDto> ConvertToFileDiffs(List<DiffFileDto> parsedDiff)
    {
        return parsedDiff.Select(df => new FileDiffDto
        {
            FilePath = df.NewPath ?? df.OldPath,
            AddedLines = df.Hunks.Sum(h => h.Changes.Count(c => c.Type == "insert")),
            DeletedLines = df.Hunks.Sum(h => h.Changes.Count(c => c.Type == "delete")),
            IsNewFile = df.Type == "add",
            IsDeletedFile = df.Type == "delete",
            ChangeType = df.Type,
            OldPath = df.OldPath,
            NewPath = df.NewPath,
            AddedContent = df.Hunks.SelectMany(h => h.Changes.Where(c => c.Type == "insert").Select(c => c.Content)).ToList(),
            DeletedContent = df.Hunks.SelectMany(h => h.Changes.Where(c => c.Type == "delete").Select(c => c.Content)).ToList()
        }).ToList();
    }
}