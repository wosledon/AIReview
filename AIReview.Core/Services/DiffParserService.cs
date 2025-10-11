using System.Text.RegularExpressions;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;

namespace AIReview.Core.Services;

public class DiffParserService : IDiffParserService
{
    public List<DiffFileDto> ParseGitDiff(string gitDiff)
    {
        if (string.IsNullOrWhiteSpace(gitDiff))
            return new List<DiffFileDto>();

        var files = new List<DiffFileDto>();
        var lines = gitDiff.Split('\n');
        var currentFile = new DiffFileDto();
        var currentHunk = new DiffHunkDto();
        var oldLineNumber = 0;
        var newLineNumber = 0;
        var lineNumber = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Parse file header
            if (line.StartsWith("diff --git"))
            {
                // Save previous file if exists
                if (!string.IsNullOrEmpty(currentFile.NewPath))
                {
                    if (currentHunk.Changes.Any())
                    {
                        currentFile.Hunks.Add(currentHunk);
                        currentHunk = new DiffHunkDto();
                    }
                    files.Add(currentFile);
                }

                currentFile = new DiffFileDto();
                var match = Regex.Match(line, @"diff --git a/(.*?) b/(.*)");
                if (match.Success)
                {
                    currentFile.OldPath = match.Groups[1].Value;
                    currentFile.NewPath = match.Groups[2].Value;
                }
            }
            // Parse old file path
            else if (line.StartsWith("--- "))
            {
                var path = line.Substring(4);
                if (path.StartsWith("a/"))
                    currentFile.OldPath = path.Substring(2);
                else if (path == "/dev/null")
                    currentFile.Type = "add";
            }
            // Parse new file path
            else if (line.StartsWith("+++ "))
            {
                var path = line.Substring(4);
                if (path.StartsWith("b/"))
                    currentFile.NewPath = path.Substring(2);
                else if (path == "/dev/null")
                    currentFile.Type = "delete";
                
                // Determine file type if not set
                if (string.IsNullOrEmpty(currentFile.Type))
                {
                    currentFile.Type = currentFile.OldPath == currentFile.NewPath ? "modify" : "rename";
                }
            }
            // Parse hunk header
            else if (line.StartsWith("@@"))
            {
                // Save previous hunk if exists
                if (currentHunk.Changes.Any())
                {
                    currentFile.Hunks.Add(currentHunk);
                }

                currentHunk = new DiffHunkDto();
                var match = Regex.Match(line, @"@@ -(\d+),?(\d*) \+(\d+),?(\d*) @@");
                if (match.Success)
                {
                    currentHunk.OldStart = int.Parse(match.Groups[1].Value);
                    currentHunk.OldLines = string.IsNullOrEmpty(match.Groups[2].Value) ? 1 : int.Parse(match.Groups[2].Value);
                    currentHunk.NewStart = int.Parse(match.Groups[3].Value);
                    currentHunk.NewLines = string.IsNullOrEmpty(match.Groups[4].Value) ? 1 : int.Parse(match.Groups[4].Value);
                    
                    oldLineNumber = currentHunk.OldStart;
                    newLineNumber = currentHunk.NewStart;
                    lineNumber = 0;
                }
            }
            // Parse content lines
            else if (line.Length > 0)
            {
                var changeType = "normal";
                var content = line;

                if (line.StartsWith("-"))
                {
                    changeType = "delete";
                    content = line.Substring(1);
                    lineNumber++;
                    
                    currentHunk.Changes.Add(new DiffChangeDto
                    {
                        Type = changeType,
                        LineNumber = lineNumber,
                        Content = content,
                        OldLineNumber = oldLineNumber,
                        NewLineNumber = null
                    });
                    
                    oldLineNumber++;
                }
                else if (line.StartsWith("+"))
                {
                    changeType = "insert";
                    content = line.Substring(1);
                    lineNumber++;
                    
                    currentHunk.Changes.Add(new DiffChangeDto
                    {
                        Type = changeType,
                        LineNumber = lineNumber,
                        Content = content,
                        OldLineNumber = null,
                        NewLineNumber = newLineNumber
                    });
                    
                    newLineNumber++;
                }
                else if (line.StartsWith(" "))
                {
                    content = line.Substring(1);
                    lineNumber++;
                    
                    currentHunk.Changes.Add(new DiffChangeDto
                    {
                        Type = changeType,
                        LineNumber = lineNumber,
                        Content = content,
                        OldLineNumber = oldLineNumber,
                        NewLineNumber = newLineNumber
                    });
                    
                    oldLineNumber++;
                    newLineNumber++;
                }
            }
        }

        // Add the last file and hunk
        if (!string.IsNullOrEmpty(currentFile.NewPath))
        {
            if (currentHunk.Changes.Any())
            {
                currentFile.Hunks.Add(currentHunk);
            }
            files.Add(currentFile);
        }

        return files;
    }
}