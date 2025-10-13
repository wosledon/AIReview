using AIReview.Shared.DTOs;

namespace AIReview.Core.Interfaces;

/// <summary>
/// PR变更摘要服务接口
/// </summary>
public interface IPullRequestAnalysisService
{
    /// <summary>
    /// 生成PR变更摘要和影响分析
    /// </summary>
    Task<PullRequestChangeSummaryDto> GenerateChangeSummaryAsync(int reviewRequestId);
    
    /// <summary>
    /// 获取PR变更摘要
    /// </summary>
    Task<PullRequestChangeSummaryDto?> GetChangeSummaryAsync(int reviewRequestId);
    
    /// <summary>
    /// 更新PR变更摘要
    /// </summary>
    Task<PullRequestChangeSummaryDto> UpdateChangeSummaryAsync(int id, PullRequestChangeSummaryDto dto);
    
    /// <summary>
    /// 生成综合分析报告
    /// </summary>
    Task<ComprehensiveReviewAnalysisDto> GenerateComprehensiveAnalysisAsync(int reviewRequestId);
}