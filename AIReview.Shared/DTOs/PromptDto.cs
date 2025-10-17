using AIReview.Shared.Enums;

namespace AIReview.Shared.DTOs;

public class PromptDto
{
    public int Id { get; set; }
    public PromptType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public int? ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreatePromptRequest
{
    public PromptType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public int? ProjectId { get; set; }
}

public class UpdatePromptRequest
{
    public string? Name { get; set; }
    public string? Content { get; set; }
}

public class EffectivePromptResponse
{
    public PromptType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = "built-in"; // project|user|built-in
}
