using AIReview.Shared.DTOs;

namespace AIReview.Core.Interfaces
{
    /// <summary>
    /// 异步AI分析服务接口
    /// </summary>
    public interface IAsyncAnalysisService
    {
        /// <summary>
        /// 异步生成风险评估
        /// </summary>
        Task<string> EnqueueRiskAssessmentAsync(int reviewRequestId);

        /// <summary>
        /// 异步生成改进建议
        /// </summary>
        Task<string> EnqueueImprovementSuggestionsAsync(int reviewRequestId);

        /// <summary>
        /// 异步生成PR摘要
        /// </summary>
        Task<string> EnqueuePullRequestSummaryAsync(int reviewRequestId);

        /// <summary>
        /// 异步生成综合分析
        /// </summary>
        Task<string> EnqueueComprehensiveAnalysisAsync(int reviewRequestId);

        /// <summary>
        /// 获取分析任务状态
        /// </summary>
        Task<JobDetailsDto?> GetJobStatusAsync(string jobId);

        /// <summary>
        /// 取消分析任务
        /// </summary>
        Task<bool> CancelJobAsync(string jobId);
    }
}