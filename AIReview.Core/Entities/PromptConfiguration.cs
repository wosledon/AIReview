using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AIReview.Shared.Enums;

namespace AIReview.Core.Entities;

/// <summary>
/// Prompt 配置，支持按用户或按项目覆盖。二者至少设置其一。
/// 唯一性：同一 UserId+Type 或同一 ProjectId+Type 只能有一条。
/// </summary>
public class PromptConfiguration
{
    public int Id { get; set; }

    [Required]
    public PromptType Type { get; set; }

    /// <summary>
    /// 模板名称（便于管理）
    /// </summary>
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 实际模板内容（可包含占位符）
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 归属用户（用户默认模板）
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 归属项目（项目覆盖模板）
    /// </summary>
    public int? ProjectId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 导航属性（可选）
    public ApplicationUser? User { get; set; }
    public Project? Project { get; set; }
}
