using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIReview.Core.Entities;

[Table("projects")]
public class Project
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [StringLength(500)]
    public string? RepositoryUrl { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Language { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // 外键
    [Required]
    public string OwnerId { get; set; } = string.Empty;
    
    // 导航属性
    public virtual ApplicationUser Owner { get; set; } = null!;
    public virtual ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public virtual ICollection<ReviewRequest> ReviewRequests { get; set; } = new List<ReviewRequest>();
}