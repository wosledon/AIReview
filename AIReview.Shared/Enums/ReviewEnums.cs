using System.ComponentModel.DataAnnotations;

namespace AIReview.Shared.Enums;

public enum ReviewState
{
    Pending = 0,
    AIReviewing = 1,
    HumanReview = 2,
    Approved = 3,
    Rejected = 4,
    Merged = 5
}

public enum ReviewCommentSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

public enum ReviewCommentCategory
{
    Quality = 0,
    Security = 1,
    Performance = 2,
    Style = 3,
    Bug = 4
}

public enum ProjectMemberRole
{
    Owner = 0,
    Admin = 1,
    Developer = 2,
    Viewer = 3
}