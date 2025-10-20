namespace AIReview.Shared.DTOs;

/// <summary>
/// 聊天消息DTO
/// </summary>
public class ChatMessageDto
{
    /// <summary>
    /// 消息ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 消息类型
    /// </summary>
    public ChatMessageType Type { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 使用的模型ID
    /// </summary>
    public int? ModelId { get; set; }
}

/// <summary>
/// 聊天消息类型
/// </summary>
public enum ChatMessageType
{
    /// <summary>
    /// 用户消息
    /// </summary>
    User,

    /// <summary>
    /// 机器人消息
    /// </summary>
    Bot,

    /// <summary>
    /// 系统消息
    /// </summary>
    System
}

/// <summary>
/// 发送聊天消息请求DTO
/// </summary>
public class SendChatMessageRequest
{
    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 使用的模型ID
    /// </summary>
    public int ModelId { get; set; }
}

/// <summary>
/// 聊天会话DTO
/// </summary>
public class ChatSessionDto
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 会话标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 消息列表
    /// </summary>
    public List<ChatMessageDto> Messages { get; set; } = new();
}