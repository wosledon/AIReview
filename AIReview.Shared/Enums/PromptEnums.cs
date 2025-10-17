using System.ComponentModel.DataAnnotations;

namespace AIReview.Shared.Enums;

/// <summary>
/// 提示词类型（按场景分类）
/// </summary>
public enum PromptType
{
    /// <summary>
    /// 代码评审（ReviewWithAutoChunking 的任务提示）
    /// </summary>
    Review = 0,

    /// <summary>
    /// 风险分析（RiskAssessment 的任务提示）
    /// </summary>
    RiskAnalysis = 1,

    /// <summary>
    /// PR 变更摘要
    /// </summary>
    PullRequestSummary = 2,

    /// <summary>
    /// 改进建议生成
    /// </summary>
    ImprovementSuggestions = 3
}
