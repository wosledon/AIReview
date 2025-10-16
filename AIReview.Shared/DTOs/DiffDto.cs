namespace AIReview.Shared.DTOs;

public class DiffResponseDto
{
    public List<DiffFileDto> Files { get; set; } = new();
    public List<CodeCommentDto> Comments { get; set; } = new();
}

/// <summary>
/// 轻量级文件列表DTO - 只包含文件元数据，不包含diff内容
/// </summary>
public class DiffFileListDto
{
    public List<DiffFileMetadataDto> Files { get; set; } = new();
    public List<CodeCommentDto> Comments { get; set; } = new();
    public int TotalFiles { get; set; }
    public int TotalAddedLines { get; set; }
    public int TotalDeletedLines { get; set; }
}

/// <summary>
/// 文件元数据DTO - 不包含具体的diff内容
/// </summary>
public class DiffFileMetadataDto
{
    public string OldPath { get; set; } = "";
    public string NewPath { get; set; } = "";
    public string Type { get; set; } = ""; // add, delete, modify, rename
    public int AddedLines { get; set; }
    public int DeletedLines { get; set; }
    public int TotalChanges { get; set; } // hunk数量
}

/// <summary>
/// 单个文件的完整diff内容DTO
/// </summary>
public class DiffFileDetailDto
{
    public DiffFileDto File { get; set; } = new();
    public List<CodeCommentDto> Comments { get; set; } = new(); // 该文件相关的评论
}


/// <summary>
/// 文件差异DTO - 用于分析服务
/// </summary>
public class FileDiffDto
{
    public string FilePath { get; set; } = string.Empty;
    public int AddedLines { get; set; }
    public int DeletedLines { get; set; }
    public bool IsNewFile { get; set; }
    public bool IsDeletedFile { get; set; }
    public string ChangeType { get; set; } = string.Empty; // "added", "deleted", "modified", "renamed"
    public string? OldPath { get; set; }
    public string? NewPath { get; set; }
    public List<string> AddedContent { get; set; } = new();
    public List<string> DeletedContent { get; set; } = new();
}

public class DiffFileDto
{
    public string OldPath { get; set; } = "";
    public string NewPath { get; set; } = "";
    public string Type { get; set; } = ""; // add, delete, modify, rename
    public List<DiffHunkDto> Hunks { get; set; } = new();
}

public class DiffHunkDto
{
    public int OldStart { get; set; }
    public int OldLines { get; set; }
    public int NewStart { get; set; }
    public int NewLines { get; set; }
    public List<DiffChangeDto> Changes { get; set; } = new();
}

public class DiffChangeDto
{
    public string Type { get; set; } = ""; // insert, delete, normal
    public int LineNumber { get; set; }
    public string Content { get; set; } = "";
    public int? OldLineNumber { get; set; }
    public int? NewLineNumber { get; set; }
}

public class CodeCommentDto
{
    public string Id { get; set; } = "";
    public string FilePath { get; set; } = "";
    public int LineNumber { get; set; }
    public string Content { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Type { get; set; } = ""; // human, ai
    public string? Severity { get; set; } // info, warning, error, critical
}