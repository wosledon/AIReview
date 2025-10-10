using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AIReview.Shared.Enums;

namespace AIReview.Core.Entities;

[Table("review_comments")]
public class ReviewComment
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ReviewRequestId { get; set; }
    
    [Required]
    public string AuthorId { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? FilePath { get; set; }
    
    public int? LineNumber { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public ReviewCommentSeverity Severity { get; set; } = ReviewCommentSeverity.Info;
    
    public ReviewCommentCategory Category { get; set; } = ReviewCommentCategory.Quality;
    
    public bool IsAIGenerated { get; set; } = false;
    
    public string? Suggestion { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // 导航属性
    public virtual ReviewRequest ReviewRequest { get; set; } = null!;
    public virtual ApplicationUser Author { get; set; } = null!;
}