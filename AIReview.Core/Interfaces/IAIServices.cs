using AIReview.Shared.DTOs;

namespace AIReview.Core.Interfaces;

public class AIReviewResult
{
    public double OverallScore { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<AIReviewComment> Comments { get; set; } = new();
    public List<string> ActionableItems { get; set; } = new();
}

public class AIReviewComment
{
    public string? FilePath { get; set; }
    public int? LineNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
    public string Category { get; set; } = "quality";
    public string? Suggestion { get; set; }
}

public class ReviewContext
{
    public string Language { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public string CodingStandards { get; set; } = string.Empty;
}

public interface IAIReviewer
{
    Task<AIReviewResult> ReviewCodeAsync(string diff, ReviewContext context);
}

public interface ILLMService
{
    Task<string> GenerateAsync(string prompt);
    Task<bool> TestConnectionAsync();
}

public interface IContextBuilder
{
    Task<ReviewContext> BuildContextAsync(string diff, ReviewContext baseContext);
}