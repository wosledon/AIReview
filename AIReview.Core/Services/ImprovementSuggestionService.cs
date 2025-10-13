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
            var response = await _llmService.GenerateAnalysisAsync(prompt, codeToAnalyze);
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
            var response = await _llmService.GenerateAnalysisAsync(prompt, fullDiff);
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
            // 预处理：去除 markdown 代码块包裹
            var cleanResponse = CleanJsonResponse(response);
            var jsonArray = JsonSerializer.Deserialize<JsonElement[]>(cleanResponse);
            return jsonArray?.Select(json => new AISuggestion
            {
                StartLine = json.TryGetProperty("startLine", out var startLine) ? startLine.GetInt32() : (int?)null,
                EndLine = json.TryGetProperty("endLine", out var endLine) ? endLine.GetInt32() : (int?)null,
                Type = Enum.Parse<ImprovementType>(json.GetProperty("type").GetString()!),
                Priority = Enum.Parse<ImprovementPriority>(json.GetProperty("priority").GetString()!),
                Title = json.GetProperty("title").GetString()!,
                Description = json.GetProperty("description").GetString()!,
                OriginalCode = json.TryGetProperty("originalCode", out var original) ? original.GetString() : null,
                SuggestedCode = json.TryGetProperty("suggestedCode", out var suggested) ? suggested.GetString() : null,
                Reasoning = json.TryGetProperty("reasoning", out var reasoning) ? reasoning.GetString() : null,
                ExpectedBenefits = json.TryGetProperty("expectedBenefits", out var benefits) ? benefits.GetString() : null,
                ImplementationComplexity = json.GetProperty("implementationComplexity").GetInt32(),
                ImpactAssessment = json.TryGetProperty("impactAssessment", out var impact) ? impact.GetString() : null,
                ConfidenceScore = json.GetProperty("confidenceScore").GetDouble()
            }).ToList() ?? new List<AISuggestion>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI suggestions response");
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

        var trimmed = response.Trim();

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