using System.ComponentModel.DataAnnotations;

namespace AIReview.Shared.DTOs;

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RepositoryUrl { get; set; }
    public string Language { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateProjectRequest
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Url]
    public string? RepositoryUrl { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Language { get; set; } = string.Empty;
}

public class UpdateProjectRequest
{
    [StringLength(255)]
    public string? Name { get; set; }
    
    public string? Description { get; set; }
    
    [Url]
    public string? RepositoryUrl { get; set; }
    
    [StringLength(50)]
    public string? Language { get; set; }
}

public class ProjectMemberDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class AddMemberRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Role { get; set; } = "Developer";
}