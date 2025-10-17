namespace AIReview.Infrastructure.Services;

/// <summary>
/// 分块评审/分析的运行时可配置项
/// </summary>
public class ChunkedReviewOptions
{
    /// <summary>
    /// 最大并发请求数量（默认 4）
    /// </summary>
    public int MaxConcurrency { get; set; } = 10;

    /// <summary>
    /// 单个分块请求超时时长（秒，默认 120s）
    /// </summary>
    public int PerChunkTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// 失败重试次数（默认 1 次，不含首轮）
    /// </summary>
    public int MaxRetries { get; set; } = 1;

    /// <summary>
    /// 初始重试延迟（毫秒，默认 500ms），后续指数回退
    /// </summary>
    public int InitialRetryDelayMs { get; set; } = 500;
}
