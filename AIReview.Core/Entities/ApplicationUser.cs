using Microsoft.AspNetCore.Identity;

namespace AIReview.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public string? Avatar { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // 导航属性
    public virtual ICollection<Project> OwnedProjects { get; set; } = new List<Project>();
    public virtual ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
    public virtual ICollection<ReviewRequest> AuthoredReviews { get; set; } = new List<ReviewRequest>();
    public virtual ICollection<ReviewComment> ReviewComments { get; set; } = new List<ReviewComment>();
}