using Microsoft.Extensions.Logging;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;
using System.Text.Json;

namespace AIReview.Core.Services;

public class RiskAssessmentService : IRiskAssessmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGitService _gitService;
    private readonly IDiffParserService _diffParserService;
    private readonly IMultiLLMService _llmService;
    private readonly ILogger<RiskAssessmentService> _logger;

    public RiskAssessmentService(
        IUnitOfWork unitOfWork,
        IGitService gitService,
        IDiffParserService diffParserService,
        IMultiLLMService llmService,
        ILogger<RiskAssessmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _gitService = gitService;
        _diffParserService = diffParserService;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<RiskAssessmentDto> GenerateRiskAssessmentAsync(int reviewRequestId)
    {
        var reviewRequest = await _unitOfWork.ReviewRequests.GetReviewWithProjectAsync(reviewRequestId);
        if (reviewRequest == null)
            throw new ArgumentException($"Review request with id {reviewRequestId} not found");

        if (reviewRequest.Project == null)
            throw new InvalidOperationException($"Project not found for review request {reviewRequestId}");

        _logger.LogInformation("Generating risk assessment for review {ReviewId}", reviewRequestId);

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
                throw new InvalidOperationException("No code changes found to analyze");
            }

            // 解析差异
            var parsedDiff = _diffParserService.ParseGitDiff(diff);
            var fileDiffs = ConvertToFileDiffs(parsedDiff);

            // 计算基础风险指标
            var riskMetrics = CalculateBasicRiskMetrics(fileDiffs);

            // 使用AI进行深度风险分析
            var aiAnalysis = await PerformAIRiskAnalysisAsync(fileDiffs, diff);

            // 创建风险评估实体
            var riskAssessment = new RiskAssessment
            {
                ReviewRequestId = reviewRequestId,
                OverallRiskScore = CalculateOverallRiskScore(riskMetrics, aiAnalysis),
                ComplexityRisk = riskMetrics.ComplexityRisk,
                SecurityRisk = aiAnalysis.SecurityRisk,
                PerformanceRisk = aiAnalysis.PerformanceRisk,
                MaintainabilityRisk = riskMetrics.MaintainabilityRisk,
                TestCoverageRisk = riskMetrics.TestCoverageRisk,
                ChangedFilesCount = riskMetrics.ChangedFilesCount,
                ChangedLinesCount = riskMetrics.ChangedLinesCount,
                RiskDescription = aiAnalysis.RiskDescription,
                MitigationSuggestions = aiAnalysis.MitigationSuggestions,
                AIModelVersion = aiAnalysis.ModelVersion,
                ConfidenceScore = aiAnalysis.ConfidenceScore,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 检查是否已存在风险评估
            var existingAssessment = await _unitOfWork.RiskAssessments.GetByReviewRequestIdAsync(reviewRequestId);
            if (existingAssessment != null)
            {
                // 更新现有评估
                existingAssessment.OverallRiskScore = riskAssessment.OverallRiskScore;
                existingAssessment.ComplexityRisk = riskAssessment.ComplexityRisk;
                existingAssessment.SecurityRisk = riskAssessment.SecurityRisk;
                existingAssessment.PerformanceRisk = riskAssessment.PerformanceRisk;
                existingAssessment.MaintainabilityRisk = riskAssessment.MaintainabilityRisk;
                existingAssessment.TestCoverageRisk = riskAssessment.TestCoverageRisk;
                existingAssessment.ChangedFilesCount = riskAssessment.ChangedFilesCount;
                existingAssessment.ChangedLinesCount = riskAssessment.ChangedLinesCount;
                existingAssessment.RiskDescription = riskAssessment.RiskDescription;
                existingAssessment.MitigationSuggestions = riskAssessment.MitigationSuggestions;
                existingAssessment.AIModelVersion = riskAssessment.AIModelVersion;
                existingAssessment.ConfidenceScore = riskAssessment.ConfidenceScore;
                existingAssessment.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.RiskAssessments.Update(existingAssessment);
                riskAssessment = existingAssessment;
            }
            else
            {
                await _unitOfWork.RiskAssessments.AddAsync(riskAssessment);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Risk assessment generated for review {ReviewId} with overall score {Score}", 
                reviewRequestId, riskAssessment.OverallRiskScore);

            return MapToDto(riskAssessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate risk assessment for review {ReviewId}", reviewRequestId);
            throw;
        }
    }

    public async Task<RiskAssessmentDto?> GetRiskAssessmentAsync(int reviewRequestId)
    {
        var riskAssessment = await _unitOfWork.RiskAssessments.GetByReviewRequestIdAsync(reviewRequestId);
        return riskAssessment != null ? MapToDto(riskAssessment) : null;
    }

    public async Task<RiskAssessmentDto> UpdateRiskAssessmentAsync(int id, RiskAssessmentDto dto)
    {
        var riskAssessment = await _unitOfWork.RiskAssessments.GetByIdAsync(id);
        if (riskAssessment == null)
            throw new ArgumentException($"Risk assessment with id {id} not found");

        // 更新可编辑字段
        riskAssessment.RiskDescription = dto.RiskDescription;
        riskAssessment.MitigationSuggestions = dto.MitigationSuggestions;
        riskAssessment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.RiskAssessments.Update(riskAssessment);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(riskAssessment);
    }

    private BasicRiskMetrics CalculateBasicRiskMetrics(List<FileDiffDto> parsedDiff)
    {
        var metrics = new BasicRiskMetrics();

        metrics.ChangedFilesCount = parsedDiff.Count;
        metrics.ChangedLinesCount = parsedDiff.Sum(f => f.AddedLines + f.DeletedLines);

        // 计算复杂度风险 (基于文件数量和行数变更)
        metrics.ComplexityRisk = Math.Min(100, 
            (metrics.ChangedFilesCount * 5) + (metrics.ChangedLinesCount / 10.0));

        // 计算可维护性风险 (基于文件类型和变更分布)
        var configFiles = parsedDiff.Count(f => IsConfigurationFile(f.FilePath));
        var coreFiles = parsedDiff.Count(f => IsCoreBusinessFile(f.FilePath));
        
        metrics.MaintainabilityRisk = Math.Min(100, 
            (configFiles * 15) + (coreFiles * 10) + (metrics.ChangedFilesCount * 2));

        // 计算测试覆盖率风险 (基于测试文件比例)
        var testFiles = parsedDiff.Count(f => IsTestFile(f.FilePath));
        var nonTestFiles = metrics.ChangedFilesCount - testFiles;
        var testCoverageRatio = nonTestFiles > 0 ? (double)testFiles / nonTestFiles : 1.0;
        
        metrics.TestCoverageRisk = Math.Max(0, 100 - (testCoverageRatio * 50));

        return metrics;
    }

    private async Task<AIRiskAnalysis> PerformAIRiskAnalysisAsync(List<FileDiffDto> parsedDiff, string rawDiff)
    {
        var prompt = BuildRiskAnalysisPrompt(parsedDiff, rawDiff);
        
        try
        {
            var response = await _llmService.GenerateAnalysisAsync(prompt, rawDiff);
            return ParseAIRiskAnalysis(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI risk analysis failed, using fallback values");
            return GetFallbackRiskAnalysis();
        }
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

    private string BuildRiskAnalysisPrompt(List<FileDiffDto> parsedDiff, string rawDiff)
    {
        var filesSummary = string.Join("\n", parsedDiff.Select(f => 
            $"- {f.FilePath}: +{f.AddedLines}/-{f.DeletedLines} lines"));

        return $@"分析以下代码变更的风险等级，并提供详细的风险评估和缓解建议。

变更文件摘要:
{filesSummary}

代码差异 (前1000字符):
{rawDiff.Substring(0, Math.Min(1000, rawDiff.Length))}

请提供以下评估结果 (JSON格式):
{{
    ""securityRisk"": 0-100,  // 安全风险评分
    ""performanceRisk"": 0-100, // 性能风险评分
    ""riskDescription"": ""详细的风险描述"",
    ""mitigationSuggestions"": ""具体的缓解建议"",
    ""confidenceScore"": 0.0-1.0 // 评估置信度
}}

注意:
- 重点关注安全漏洞、性能瓶颈、架构问题
- 考虑变更对系统稳定性的影响
- 提供具体可行的改进建议";
    }

    private AIRiskAnalysis ParseAIRiskAnalysis(string response)
    {
        try
        {
            // 预处理：去除 markdown 代码块包裹
            var cleanResponse = CleanJsonResponse(response);
            
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(cleanResponse);
            
            return new AIRiskAnalysis
            {
                SecurityRisk = jsonResponse.GetProperty("securityRisk").GetDouble(),
                PerformanceRisk = jsonResponse.GetProperty("performanceRisk").GetDouble(),
                RiskDescription = jsonResponse.GetProperty("riskDescription").GetString(),
                MitigationSuggestions = jsonResponse.GetProperty("mitigationSuggestions").GetString(),
                ConfidenceScore = jsonResponse.GetProperty("confidenceScore").GetDouble(),
                ModelVersion = "gpt-4" // 这里应该从LLM服务获取实际的模型版本
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI risk analysis response");
            return GetFallbackRiskAnalysis();
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

    private AIRiskAnalysis GetFallbackRiskAnalysis()
    {
        return new AIRiskAnalysis
        {
            SecurityRisk = 30,
            PerformanceRisk = 25,
            RiskDescription = "无法完成AI风险分析，使用默认评估",
            MitigationSuggestions = "建议进行人工代码审查",
            ConfidenceScore = 0.1,
            ModelVersion = "fallback"
        };
    }

    private double CalculateOverallRiskScore(BasicRiskMetrics basic, AIRiskAnalysis ai)
    {
        // 加权平均计算整体风险评分
        var weights = new Dictionary<string, double>
        {
            ["complexity"] = 0.2,
            ["security"] = 0.25,
            ["performance"] = 0.2,
            ["maintainability"] = 0.15,
            ["testCoverage"] = 0.2
        };

        return (basic.ComplexityRisk * weights["complexity"]) +
               (ai.SecurityRisk * weights["security"]) +
               (ai.PerformanceRisk * weights["performance"]) +
               (basic.MaintainabilityRisk * weights["maintainability"]) +
               (basic.TestCoverageRisk * weights["testCoverage"]);
    }

    private bool IsConfigurationFile(string filePath)
    {
        var configExtensions = new[] { ".json", ".xml", ".yaml", ".yml", ".config", ".ini", ".env" };
        var configNames = new[] { "appsettings", "web.config", "app.config", "dockerfile", "docker-compose" };
        
        return configExtensions.Any(ext => filePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
               configNames.Any(name => filePath.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsCoreBusinessFile(string filePath)
    {
        var coreKeywords = new[] { "service", "controller", "repository", "entity", "model", "business", "core" };
        return coreKeywords.Any(keyword => filePath.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsTestFile(string filePath)
    {
        var testKeywords = new[] { "test", "spec", "tests" };
        return testKeywords.Any(keyword => filePath.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private RiskAssessmentDto MapToDto(RiskAssessment entity)
    {
        return new RiskAssessmentDto
        {
            Id = entity.Id,
            ReviewRequestId = entity.ReviewRequestId,
            OverallRiskScore = entity.OverallRiskScore,
            ComplexityRisk = entity.ComplexityRisk,
            SecurityRisk = entity.SecurityRisk,
            PerformanceRisk = entity.PerformanceRisk,
            MaintainabilityRisk = entity.MaintainabilityRisk,
            TestCoverageRisk = entity.TestCoverageRisk,
            ChangedFilesCount = entity.ChangedFilesCount,
            ChangedLinesCount = entity.ChangedLinesCount,
            RiskDescription = entity.RiskDescription,
            MitigationSuggestions = entity.MitigationSuggestions,
            AIModelVersion = entity.AIModelVersion,
            ConfidenceScore = entity.ConfidenceScore,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private class BasicRiskMetrics
    {
        public int ChangedFilesCount { get; set; }
        public int ChangedLinesCount { get; set; }
        public double ComplexityRisk { get; set; }
        public double MaintainabilityRisk { get; set; }
        public double TestCoverageRisk { get; set; }
    }

    private class AIRiskAnalysis
    {
        public double SecurityRisk { get; set; }
        public double PerformanceRisk { get; set; }
        public string? RiskDescription { get; set; }
        public string? MitigationSuggestions { get; set; }
        public double ConfidenceScore { get; set; }
        public string? ModelVersion { get; set; }
    }
}