using Microsoft.Extensions.Logging;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;
using AIReview.Shared.Enums;
using System.Text.Json;

namespace AIReview.Core.Services;

public class PullRequestAnalysisService : IPullRequestAnalysisService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGitService _gitService;
    private readonly IDiffParserService _diffParserService;
    private readonly IMultiLLMService _llmService;
    private readonly IRiskAssessmentService _riskAssessmentService;
    private readonly IImprovementSuggestionService _improvementSuggestionService;
    private readonly ILogger<PullRequestAnalysisService> _logger;

    public PullRequestAnalysisService(
        IUnitOfWork unitOfWork,
        IGitService gitService,
        IDiffParserService diffParserService,
        IMultiLLMService llmService,
        IRiskAssessmentService riskAssessmentService,
        IImprovementSuggestionService improvementSuggestionService,
        ILogger<PullRequestAnalysisService> logger)
    {
        _unitOfWork = unitOfWork;
        _gitService = gitService;
        _diffParserService = diffParserService;
        _llmService = llmService;
        _riskAssessmentService = riskAssessmentService;
        _improvementSuggestionService = improvementSuggestionService;
        _logger = logger;
    }

    public async Task<PullRequestChangeSummaryDto> GenerateChangeSummaryAsync(int reviewRequestId)
    {
        var reviewRequest = await _unitOfWork.ReviewRequests.GetReviewWithProjectAsync(reviewRequestId);
        if (reviewRequest == null)
            throw new ArgumentException($"Review request with id {reviewRequestId} not found");

        _logger.LogInformation("Generating PR change summary for review {ReviewId}", reviewRequestId);

        try
        {
            // 获取项目的仓库
            var repository = await _gitService.GetRepositoryByUrlAsync(reviewRequest.Project.RepositoryUrl ?? "");
            if (repository == null)
            {
                throw new InvalidOperationException($"Repository not found for project {reviewRequest.Project.Name}");
            }

            // 获取代码差异
            var diffResult = await _gitService.GetDiffBetweenRefsAsync(
                repository.Id,
                reviewRequest.BaseBranch,
                reviewRequest.Branch
            );

            if (diffResult == null)
            {
                throw new InvalidOperationException("Failed to get diff between branches");
            }

            // 解析差异
            var parsedDiff = _diffParserService.ParseGitDiff(diffResult);

            // 转换为分析服务所需的格式
            var fileDiffs = ConvertToFileDiffDtos(parsedDiff);

            // 计算变更统计信息
            var changeStats = CalculateChangeStatistics(fileDiffs);

            // 使用AI分析变更内容和影响
            var aiAnalysis = await PerformAIChangeAnalysisAsync(fileDiffs, diffResult, reviewRequest);

            // 创建或更新PR变更摘要
            var changeSummary = new PullRequestChangeSummary
            {
                ReviewRequestId = reviewRequestId,
                ChangeType = aiAnalysis.ChangeType,
                Summary = aiAnalysis.Summary,
                DetailedDescription = aiAnalysis.DetailedDescription,
                KeyChanges = aiAnalysis.KeyChanges,
                ImpactAnalysis = aiAnalysis.ImpactAnalysis,
                BusinessImpact = aiAnalysis.BusinessImpact,
                TechnicalImpact = aiAnalysis.TechnicalImpact,
                BreakingChangeRisk = aiAnalysis.BreakingChangeRisk,
                TestingRecommendations = aiAnalysis.TestingRecommendations,
                DeploymentConsiderations = aiAnalysis.DeploymentConsiderations,
                DependencyChanges = aiAnalysis.DependencyChanges,
                PerformanceImpact = aiAnalysis.PerformanceImpact,
                SecurityImpact = aiAnalysis.SecurityImpact,
                BackwardCompatibility = aiAnalysis.BackwardCompatibility,
                DocumentationRequirements = aiAnalysis.DocumentationRequirements,
                ChangeStatistics = JsonSerializer.Serialize(changeStats),
                AIModelVersion = aiAnalysis.ModelVersion,
                ConfidenceScore = aiAnalysis.ConfidenceScore,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 检查是否已存在变更摘要
            var existingSummary = await _unitOfWork.PullRequestChangeSummaries.GetByReviewRequestIdAsync(reviewRequestId);
            if (existingSummary != null)
            {
                // 更新现有摘要
                UpdateExistingSummary(existingSummary, changeSummary);
                _unitOfWork.PullRequestChangeSummaries.Update(existingSummary);
                changeSummary = existingSummary;
            }
            else
            {
                await _unitOfWork.PullRequestChangeSummaries.AddAsync(changeSummary);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("PR change summary generated for review {ReviewId}", reviewRequestId);

            return MapToDto(changeSummary, changeStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PR change summary for review {ReviewId}", reviewRequestId);
            throw;
        }
    }

    public async Task<PullRequestChangeSummaryDto?> GetChangeSummaryAsync(int reviewRequestId)
    {
        var changeSummary = await _unitOfWork.PullRequestChangeSummaries.GetByReviewRequestIdAsync(reviewRequestId);
        if (changeSummary == null)
            return null;

        var changeStats = !string.IsNullOrEmpty(changeSummary.ChangeStatistics)
            ? JsonSerializer.Deserialize<ChangeStatisticsDto>(changeSummary.ChangeStatistics)
            : new ChangeStatisticsDto();

        return MapToDto(changeSummary, changeStats);
    }

    public async Task<PullRequestChangeSummaryDto> UpdateChangeSummaryAsync(int id, PullRequestChangeSummaryDto dto)
    {
        var changeSummary = await _unitOfWork.PullRequestChangeSummaries.GetByIdAsync(id);
        if (changeSummary == null)
            throw new ArgumentException($"PR change summary with id {id} not found");

        // 更新可编辑字段
        changeSummary.Summary = dto.Summary;
        changeSummary.DetailedDescription = dto.DetailedDescription;
        changeSummary.TestingRecommendations = dto.TestingRecommendations;
        changeSummary.DeploymentConsiderations = dto.DeploymentConsiderations;
        changeSummary.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.PullRequestChangeSummaries.Update(changeSummary);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(changeSummary, dto.ChangeStatistics ?? new ChangeStatisticsDto());
    }

    public async Task<ComprehensiveReviewAnalysisDto> GenerateComprehensiveAnalysisAsync(int reviewRequestId)
    {
        _logger.LogInformation("Generating comprehensive analysis for review {ReviewId}", reviewRequestId);

        // 并行生成所有分析
        var riskAssessmentTask = _riskAssessmentService.GenerateRiskAssessmentAsync(reviewRequestId);
        var changeSummaryTask = GenerateChangeSummaryAsync(reviewRequestId);
        var improvementSuggestionsTask = _improvementSuggestionService.GenerateImprovementSuggestionsAsync(reviewRequestId);

        await Task.WhenAll(riskAssessmentTask, changeSummaryTask, improvementSuggestionsTask);

        var riskAssessment = await riskAssessmentTask;
        var changeSummary = await changeSummaryTask;
        var improvementSuggestions = await improvementSuggestionsTask;

        return new ComprehensiveReviewAnalysisDto
        {
            RiskAssessment = riskAssessment,
            ChangeSummary = changeSummary,
            ImprovementSuggestions = improvementSuggestions,
            GeneratedAt = DateTime.UtcNow,
            AnalysisVersion = "1.0"
        };
    }

    private ChangeStatisticsDto CalculateChangeStatistics(List<FileDiffDto> parsedDiff)
    {
        var stats = new ChangeStatisticsDto
        {
            TotalFiles = parsedDiff.Count,
            AddedFiles = parsedDiff.Count(f => f.IsNewFile),
            ModifiedFiles = parsedDiff.Count(f => !f.IsNewFile && !f.IsDeletedFile),
            DeletedFiles = parsedDiff.Count(f => f.IsDeletedFile),
            TotalLines = parsedDiff.Sum(f => f.AddedLines + f.DeletedLines),
            AddedLines = parsedDiff.Sum(f => f.AddedLines),
            DeletedLines = parsedDiff.Sum(f => f.DeletedLines)
        };

        // 按文件类型分组
        stats.FileTypeBreakdown = parsedDiff
            .GroupBy(f => Path.GetExtension(f.FilePath).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count());

        // 按语言分组
        stats.LanguageBreakdown = parsedDiff
            .GroupBy(f => GetLanguageFromExtension(Path.GetExtension(f.FilePath)))
            .ToDictionary(g => g.Key, g => g.Count());

        return stats;
    }

    private async Task<AIChangeAnalysis> PerformAIChangeAnalysisAsync(List<FileDiffDto> parsedDiff, string rawDiff, ReviewRequest reviewRequest)
    {
        var prompt = BuildChangeAnalysisPrompt(parsedDiff, rawDiff, reviewRequest);
        
        try
        {
            var response = await _llmService.GenerateAnalysisAsync(prompt, rawDiff);
            return ParseAIChangeAnalysis(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI change analysis failed, using fallback values");
            return GetFallbackChangeAnalysis(parsedDiff);
        }
    }

    private string BuildChangeAnalysisPrompt(List<FileDiffDto> parsedDiff, string rawDiff, ReviewRequest reviewRequest)
    {
        var filesSummary = string.Join("\n", parsedDiff.Select(f => 
            $"- {f.FilePath}: +{f.AddedLines}/-{f.DeletedLines} lines {(f.IsNewFile ? "(新文件)" : "")}"));

        return $@"分析以下Pull Request的变更内容，生成结构化的变更摘要和影响分析。

PR信息:
- 标题: {reviewRequest.Title}
- 描述: {reviewRequest.Description ?? "无描述"}
- 分支: {reviewRequest.Branch} -> {reviewRequest.BaseBranch}

变更文件摘要:
{filesSummary}

代码差异 (前2000字符):
{rawDiff.Substring(0, Math.Min(2000, rawDiff.Length))}

请提供详细的分析结果 (JSON格式):
{{
    ""changeType"": ""Feature|BugFix|Refactor|Performance|Security|Documentation|Test|Configuration|Dependency|Breaking"",
    ""summary"": ""简洁的变更摘要 (1-2句话)"",
    ""detailedDescription"": ""详细的变更描述"",
    ""keyChanges"": ""主要变更点列表"",
    ""impactAnalysis"": ""影响范围分析"",
    ""businessImpact"": ""None|Low|Medium|High|Critical"",
    ""technicalImpact"": ""None|Low|Medium|High|Critical"",
    ""breakingChangeRisk"": ""None|Low|Medium|High|Critical"",
    ""testingRecommendations"": ""测试重点建议"",
    ""deploymentConsiderations"": ""部署注意事项"",
    ""dependencyChanges"": ""依赖关系变更"",
    ""performanceImpact"": ""性能影响评估"",
    ""securityImpact"": ""安全影响评估"",
    ""backwardCompatibility"": ""向后兼容性分析"",
    ""documentationRequirements"": ""文档更新需求"",
    ""confidenceScore"": 0.0-1.0
}}

分析要点:
- 识别变更的主要类型和目的
- 评估对系统各层面的影响
- 提供具体的测试和部署建议
- 考虑向后兼容性和破坏性变更风险";
    }

    private AIChangeAnalysis ParseAIChangeAnalysis(string response)
    {
        try
        {
            // 预处理：去除 markdown 代码块包裹
            var cleanResponse = CleanJsonResponse(response);
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(cleanResponse);
            
            return new AIChangeAnalysis
            {
                ChangeType = Enum.Parse<ChangeType>(jsonResponse.GetProperty("changeType").GetString()!),
                Summary = jsonResponse.GetProperty("summary").GetString()!,
                DetailedDescription = jsonResponse.GetProperty("detailedDescription").GetString(),
                KeyChanges = jsonResponse.GetProperty("keyChanges").GetString(),
                ImpactAnalysis = jsonResponse.GetProperty("impactAnalysis").GetString(),
                BusinessImpact = Enum.Parse<BusinessImpact>(jsonResponse.GetProperty("businessImpact").GetString()!),
                TechnicalImpact = Enum.Parse<TechnicalImpact>(jsonResponse.GetProperty("technicalImpact").GetString()!),
                BreakingChangeRisk = Enum.Parse<BreakingChangeRisk>(jsonResponse.GetProperty("breakingChangeRisk").GetString()!),
                TestingRecommendations = jsonResponse.GetProperty("testingRecommendations").GetString(),
                DeploymentConsiderations = jsonResponse.GetProperty("deploymentConsiderations").GetString(),
                DependencyChanges = jsonResponse.GetProperty("dependencyChanges").GetString(),
                PerformanceImpact = jsonResponse.GetProperty("performanceImpact").GetString(),
                SecurityImpact = jsonResponse.GetProperty("securityImpact").GetString(),
                BackwardCompatibility = jsonResponse.GetProperty("backwardCompatibility").GetString(),
                DocumentationRequirements = jsonResponse.GetProperty("documentationRequirements").GetString(),
                ConfidenceScore = jsonResponse.GetProperty("confidenceScore").GetDouble(),
                ModelVersion = "gpt-4" // 应该从LLM服务获取
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI change analysis response");
            return GetFallbackChangeAnalysis(new List<FileDiffDto>());
        }
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

    private AIChangeAnalysis GetFallbackChangeAnalysis(List<FileDiffDto> parsedDiff)
    {
        var changeType = DetermineChangeTypeFromFiles(parsedDiff);
        
        return new AIChangeAnalysis
        {
            ChangeType = changeType,
            Summary = "代码变更摘要分析中，请稍后查看详细结果",
            DetailedDescription = "由于AI分析服务暂时不可用，请进行人工代码审查",
            BusinessImpact = BusinessImpact.Medium,
            TechnicalImpact = TechnicalImpact.Medium,
            BreakingChangeRisk = BreakingChangeRisk.Low,
            TestingRecommendations = "建议进行完整的回归测试",
            ConfidenceScore = 0.1,
            ModelVersion = "fallback"
        };
    }

    private ChangeType DetermineChangeTypeFromFiles(List<FileDiffDto> parsedDiff)
    {
        // 基于文件路径和类型推断变更类型
        if (parsedDiff.Any(f => f.FilePath.Contains("test", StringComparison.OrdinalIgnoreCase)))
            return ChangeType.Test;
        
        if (parsedDiff.Any(f => f.FilePath.EndsWith(".md") || f.FilePath.Contains("doc", StringComparison.OrdinalIgnoreCase)))
            return ChangeType.Documentation;
        
        if (parsedDiff.Any(f => f.FilePath.Contains("config", StringComparison.OrdinalIgnoreCase) || 
                                f.FilePath.EndsWith(".json") || f.FilePath.EndsWith(".xml")))
            return ChangeType.Configuration;
        
        return ChangeType.Feature; // 默认为功能变更
    }

    private void UpdateExistingSummary(PullRequestChangeSummary existing, PullRequestChangeSummary newSummary)
    {
        existing.ChangeType = newSummary.ChangeType;
        existing.Summary = newSummary.Summary;
        existing.DetailedDescription = newSummary.DetailedDescription;
        existing.KeyChanges = newSummary.KeyChanges;
        existing.ImpactAnalysis = newSummary.ImpactAnalysis;
        existing.BusinessImpact = newSummary.BusinessImpact;
        existing.TechnicalImpact = newSummary.TechnicalImpact;
        existing.BreakingChangeRisk = newSummary.BreakingChangeRisk;
        existing.TestingRecommendations = newSummary.TestingRecommendations;
        existing.DeploymentConsiderations = newSummary.DeploymentConsiderations;
        existing.DependencyChanges = newSummary.DependencyChanges;
        existing.PerformanceImpact = newSummary.PerformanceImpact;
        existing.SecurityImpact = newSummary.SecurityImpact;
        existing.BackwardCompatibility = newSummary.BackwardCompatibility;
        existing.DocumentationRequirements = newSummary.DocumentationRequirements;
        existing.ChangeStatistics = newSummary.ChangeStatistics;
        existing.AIModelVersion = newSummary.AIModelVersion;
        existing.ConfidenceScore = newSummary.ConfidenceScore;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    private string GetLanguageFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".cs" => "C#",
            ".js" => "JavaScript",
            ".ts" => "TypeScript",
            ".tsx" => "TypeScript",
            ".jsx" => "JavaScript",
            ".py" => "Python",
            ".java" => "Java",
            ".cpp" or ".cxx" or ".cc" => "C++",
            ".c" => "C",
            ".html" => "HTML",
            ".css" => "CSS",
            ".scss" => "SCSS",
            ".sql" => "SQL",
            ".json" => "JSON",
            ".xml" => "XML",
            ".yaml" or ".yml" => "YAML",
            ".md" => "Markdown",
            _ => "Other"
        };
    }

    private PullRequestChangeSummaryDto MapToDto(PullRequestChangeSummary entity, ChangeStatisticsDto? changeStats)
    {
        return new PullRequestChangeSummaryDto
        {
            Id = entity.Id,
            ReviewRequestId = entity.ReviewRequestId,
            ChangeType = entity.ChangeType,
            Summary = entity.Summary,
            DetailedDescription = entity.DetailedDescription,
            KeyChanges = entity.KeyChanges,
            ImpactAnalysis = entity.ImpactAnalysis,
            BusinessImpact = entity.BusinessImpact,
            TechnicalImpact = entity.TechnicalImpact,
            BreakingChangeRisk = entity.BreakingChangeRisk,
            TestingRecommendations = entity.TestingRecommendations,
            DeploymentConsiderations = entity.DeploymentConsiderations,
            DependencyChanges = entity.DependencyChanges,
            PerformanceImpact = entity.PerformanceImpact,
            SecurityImpact = entity.SecurityImpact,
            BackwardCompatibility = entity.BackwardCompatibility,
            DocumentationRequirements = entity.DocumentationRequirements,
            ChangeStatistics = changeStats,
            AIModelVersion = entity.AIModelVersion,
            ConfidenceScore = entity.ConfidenceScore,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private class AIChangeAnalysis
    {
        public ChangeType ChangeType { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string? DetailedDescription { get; set; }
        public string? KeyChanges { get; set; }
        public string? ImpactAnalysis { get; set; }
        public BusinessImpact BusinessImpact { get; set; }
        public TechnicalImpact TechnicalImpact { get; set; }
        public BreakingChangeRisk BreakingChangeRisk { get; set; }
        public string? TestingRecommendations { get; set; }
        public string? DeploymentConsiderations { get; set; }
        public string? DependencyChanges { get; set; }
        public string? PerformanceImpact { get; set; }
        public string? SecurityImpact { get; set; }
        public string? BackwardCompatibility { get; set; }
        public string? DocumentationRequirements { get; set; }
        public double ConfidenceScore { get; set; }
        public string? ModelVersion { get; set; }
    }

    /// <summary>
    /// 将DiffFileDto转换为FileDiffDto
    /// </summary>
    private List<FileDiffDto> ConvertToFileDiffDtos(List<DiffFileDto> diffFiles)
    {
        return diffFiles.Select(df => new FileDiffDto
        {
            FilePath = df.NewPath ?? df.OldPath,
            AddedLines = df.Hunks.SelectMany(h => h.Changes).Count(c => c.Type == "insert"),
            DeletedLines = df.Hunks.SelectMany(h => h.Changes).Count(c => c.Type == "delete"),
            IsNewFile = df.Type == "add",
            IsDeletedFile = df.Type == "delete",
            ChangeType = df.Type switch
            {
                "add" => "added",
                "delete" => "deleted",
                "modify" => "modified",
                "rename" => "renamed",
                _ => "modified"
            },
            OldPath = df.OldPath,
            NewPath = df.NewPath,
            AddedContent = df.Hunks.SelectMany(h => h.Changes)
                .Where(c => c.Type == "insert")
                .Select(c => c.Content)
                .ToList(),
            DeletedContent = df.Hunks.SelectMany(h => h.Changes)
                .Where(c => c.Type == "delete")
                .Select(c => c.Content)
                .ToList()
        }).ToList();
    }
}