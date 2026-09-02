// Lightweight show data shape for UI binding.
namespace MediaPlayerApp.Application.DTOs;

public class ShowSummaryDto
{
    public ShowSummaryDto(string name, string folderPath, string? posterPath)
    {
        Name = name;
        FolderPath = folderPath;
        PosterPath = posterPath;
    }

    public string Name { get; }

    public string FolderPath { get; }

    public string? PosterPath { get; }
}
