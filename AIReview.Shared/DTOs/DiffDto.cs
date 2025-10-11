namespace AIReview.Shared.DTOs;

public class DiffResponseDto
{
    public List<DiffFileDto> Files { get; set; } = new();
    public List<CodeCommentDto> Comments { get; set; } = new();
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