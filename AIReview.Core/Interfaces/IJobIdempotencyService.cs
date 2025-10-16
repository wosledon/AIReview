namespace AIReview.Core.Interfaces;

/// <summary>
/// Job幂等性保证服务接口
/// </summary>
public interface IJobIdempotencyService
{
    /// <summary>
    /// 尝试开始执行Job(幂等性检查)
    /// </summary>
    /// <param name="jobType">Job类型</param>
    /// <param name="jobKey">Job唯一标识键(如reviewId)</param>
    /// <param name="timeout">超时时间,默认30分钟</param>
    /// <returns>如果可以执行返回执行上下文,否则返回null</returns>
    Task<IJobExecutionContext?> TryStartExecutionAsync(string jobType, string jobKey, TimeSpan? timeout = null);

    /// <summary>
    /// 检查Job是否正在执行
    /// </summary>
    Task<bool> IsExecutingAsync(string jobType, string jobKey);

    /// <summary>
    /// 获取Job执行状态
    /// </summary>
    Task<JobExecutionStatus?> GetExecutionStatusAsync(string jobType, string jobKey);

    /// <summary>
    /// 清理过期的执行记录
    /// </summary>
    Task<int> CleanupExpiredExecutionsAsync(TimeSpan olderThan);
}

/// <summary>
/// Job执行上下文
/// </summary>
public interface IJobExecutionContext : IAsyncDisposable
{
    /// <summary>
    /// 执行ID
    /// </summary>
    string ExecutionId { get; }

    /// <summary>
    /// Job类型
    /// </summary>
    string JobType { get; }

    /// <summary>
    /// Job键
    /// </summary>
    string JobKey { get; }

    /// <summary>
    /// 开始时间
    /// </summary>
    DateTime StartTime { get; }

    /// <summary>
    /// 标记Job成功完成
    /// </summary>
    Task MarkSuccessAsync(object? result = null);

    /// <summary>
    /// 标记Job失败
    /// </summary>
    Task MarkFailureAsync(string errorMessage, Exception? exception = null);

    /// <summary>
    /// 更新进度
    /// </summary>
    Task UpdateProgressAsync(int progressPercentage, string? message = null);

    /// <summary>
    /// 延长执行时间(防止超时)
    /// </summary>
    Task<bool> ExtendTimeoutAsync(TimeSpan additionalTime);
}

/// <summary>
/// Job执行状态
/// </summary>
public class JobExecutionStatus
{
    /// <summary>
    /// 执行ID
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// 状态: executing, completed, failed
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 进度百分比
    /// </summary>
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// 进度消息
    /// </summary>
    public string? ProgressMessage { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 结果数据
    /// </summary>
    public object? Result { get; set; }
}
