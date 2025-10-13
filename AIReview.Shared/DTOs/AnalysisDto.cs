using AIReview.Shared.Enums;

namespace AIReview.Shared.DTOs;

/// <summary>
/// 风险评估DTO
/// </summary>
public class RiskAssessmentDto
{
    public int Id { get; set; }
    public int ReviewRequestId { get; set; }
    public double OverallRiskScore { get; set; }
    public double ComplexityRisk { get; set; }
    public double SecurityRisk { get; set; }
    public double PerformanceRisk { get; set; }
    public double MaintainabilityRisk { get; set; }
    public double TestCoverageRisk { get; set; }
    public int ChangedFilesCount { get; set; }
    public int ChangedLinesCount { get; set; }
    public string? RiskDescription { get; set; }
    public string? MitigationSuggestions { get; set; }
    public string? AIModelVersion { get; set; }
    public double? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 改进建议DTO
/// </summary>
public class ImprovementSuggestionDto
{
    public int Id { get; set; }
    public int ReviewRequestId { get; set; }
    public string? FilePath { get; set; }
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
    public bool IsAccepted { get; set; }
    public bool IsIgnored { get; set; }
    public string? UserFeedback { get; set; }
    public string? AIModelVersion { get; set; }
    public double? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// PR变更摘要DTO
/// </summary>
public class PullRequestChangeSummaryDto
{
    public int Id { get; set; }
    public int ReviewRequestId { get; set; }
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
    public ChangeStatisticsDto? ChangeStatistics { get; set; }
    public string? AIModelVersion { get; set; }
    public double? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 变更统计信息DTO
/// </summary>
public class ChangeStatisticsDto
{
    public int TotalFiles { get; set; }
    public int AddedFiles { get; set; }
    public int ModifiedFiles { get; set; }
    public int DeletedFiles { get; set; }
    public int TotalLines { get; set; }
    public int AddedLines { get; set; }
    public int DeletedLines { get; set; }
    public Dictionary<string, int> FileTypeBreakdown { get; set; } = new();
    public Dictionary<string, int> LanguageBreakdown { get; set; } = new();
}

/// <summary>
/// 综合评审分析DTO
/// </summary>
public class ComprehensiveReviewAnalysisDto
{
    public RiskAssessmentDto? RiskAssessment { get; set; }
    public PullRequestChangeSummaryDto? ChangeSummary { get; set; }
    public List<ImprovementSuggestionDto> ImprovementSuggestions { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public string? AnalysisVersion { get; set; }
}

/// <summary>
/// 接受/拒绝改进建议请求
/// </summary>
public class UpdateImprovementSuggestionRequest
{
    public bool? IsAccepted { get; set; }
    public bool? IsIgnored { get; set; }
    public string? UserFeedback { get; set; }
}