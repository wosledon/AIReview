using AIReview.Shared.DTOs;

namespace AIReview.Core.Interfaces;

public interface IDiffParserService
{
    List<DiffFileDto> ParseGitDiff(string gitDiff);
}