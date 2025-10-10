using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AIReview.Shared.Enums;

namespace AIReview.Core.Entities;

[Table("review_requests")]
public class ReviewRequest
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ProjectId { get; set; }
    
    [Required]
    public string AuthorId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Branch { get; set; } = string.Empty;
    
    [StringLength(255)]
    public string BaseBranch { get; set; } = "main";
    
    public ReviewState Status { get; set; } = ReviewState.Pending;
    
    public int? PullRequestNumber { get; set; }
    
    // AI评审结果
    public double? AIOverallScore { get; set; }
    public string? AISummary { get; set; }
    public DateTime? AIReviewedAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // 导航属性
    public virtual Project Project { get; set; } = null!;
    public virtual ApplicationUser Author { get; set; } = null!;
    public virtual ICollection<ReviewComment> Comments { get; set; } = new List<ReviewComment>();
}