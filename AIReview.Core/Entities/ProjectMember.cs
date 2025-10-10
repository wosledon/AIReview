using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AIReview.Shared.Enums;

namespace AIReview.Core.Entities;

[Table("project_members")]
public class ProjectMember
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ProjectId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public ProjectMemberRole Role { get; set; } = ProjectMemberRole.Developer;
    
    public DateTime JoinedAt { get; set; }
    
    // 导航属性
    public virtual Project Project { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;
}