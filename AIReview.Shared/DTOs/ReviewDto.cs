using System.ComponentModel.DataAnnotations;
using AIReview.Shared.Enums;

namespace AIReview.Shared.DTOs;

public class ReviewDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Branch { get; set; } = string.Empty;
    public string BaseBranch { get; set; } = string.Empty;
    public ReviewState Status { get; set; }
    public int? PullRequestNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateReviewRequest
{
    [Required]
    public int ProjectId { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Branch { get; set; } = string.Empty;
    
    [StringLength(255)]
    public string BaseBranch { get; set; } = "main";
    
    public int? PullRequestNumber { get; set; }
}

public class UpdateReviewRequest
{
    [StringLength(255)]
    public string? Title { get; set; }
    
    public string? Description { get; set; }
    
    public ReviewState? Status { get; set; }
}

public class ReviewCommentDto
{
    public int Id { get; set; }
    public int ReviewRequestId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public int? LineNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public ReviewCommentSeverity Severity { get; set; }
    public ReviewCommentCategory Category { get; set; }
    public bool IsAIGenerated { get; set; }
    public string? Suggestion { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddCommentRequest
{
    public string? FilePath { get; set; }
    
    public int? LineNumber { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public ReviewCommentSeverity Severity { get; set; } = ReviewCommentSeverity.Info;
    
    public ReviewCommentCategory Category { get; set; } = ReviewCommentCategory.Quality;
    
    public string? Suggestion { get; set; }
}

public class UpdateCommentRequest
{
    public string? Content { get; set; }
    
    public ReviewCommentSeverity? Severity { get; set; }
    
    public ReviewCommentCategory? Category { get; set; }
    
    public string? Suggestion { get; set; }
}

public class RejectReviewRequest
{
    public string? Reason { get; set; }
}

public class AIReviewResultDto
{
    public int ReviewId { get; set; }
    public double OverallScore { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<ReviewCommentDto> Comments { get; set; } = new();
    public List<string> ActionableItems { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}