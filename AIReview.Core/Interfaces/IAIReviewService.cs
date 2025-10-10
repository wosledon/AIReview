namespace AIReview.Core.Interfaces;

public interface IAIReviewService
{
    /// <summary>
    /// 排队AI评审任务
    /// </summary>
    Task<string> EnqueueReviewAsync(int reviewRequestId);

    /// <summary>
    /// 排队批量AI评审任务
    /// </summary>
    Task<string> EnqueueBulkReviewAsync(List<int> reviewRequestIds);

    /// <summary>
    /// 获取任务状态
    /// </summary>
    JobDetailsDto? GetJobStatus(string jobId);

    /// <summary>
    /// 取消任务
    /// </summary>
    bool CancelJob(string jobId);
}

public class JobDetailsDto
{
    public string Id { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? ErrorMessage { get; set; }
}