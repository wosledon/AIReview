namespace AIReview.Shared.DTOs;

public class GitRepositoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? LocalPath { get; set; }
    public string? DefaultBranch { get; set; }
    public string? Username { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public int BranchCount { get; set; }
}

public class CreateGitRepositoryDto
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? DefaultBranch { get; set; }
    public string? Username { get; set; }
    public string? AccessToken { get; set; }
    public int? ProjectId { get; set; }
}

public class UpdateGitRepositoryDto
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? DefaultBranch { get; set; }
    public string? Username { get; set; }
    public string? AccessToken { get; set; }
    public bool IsActive { get; set; }
}

public class GitBranchDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CommitSha { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GitCommitDto
{
    public int Id { get; set; }
    public string Sha { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public DateTime AuthorDate { get; set; }
    public string? CommitterName { get; set; }
    public string? CommitterEmail { get; set; }
    public DateTime? CommitterDate { get; set; }
    public string? BranchName { get; set; }
    public int FileChangesCount { get; set; }
}

public class GitCommitDetailDto : GitCommitDto
{
    public List<GitFileChangeDto> FileChanges { get; set; } = new();
}

public class GitFileChangeDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public int AddedLines { get; set; }
    public int DeletedLines { get; set; }
}