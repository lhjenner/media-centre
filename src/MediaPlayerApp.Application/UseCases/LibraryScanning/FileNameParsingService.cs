// Extracts show name, season, and episode numbers from folder and file names.
using System.Text.RegularExpressions;
using MediaPlayerApp.Domain.ValueObjects;

namespace MediaPlayerApp.Application.UseCases.LibraryScanning;

public class FileNameParsingService
{
    private static readonly Regex SeasonFolderPattern = new(@"season\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SeasonEpisodePattern = new(
        @"[Ss](?<season>\d{1,2})[Ee](?<episode>\d{1,3})",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses a season number from a folder name (e.g. "Season 2"). Returns null if no season number is found.
    /// </summary>
    public int? ParseSeason(string folderName)
    {
        var match = SeasonFolderPattern.Match(folderName);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    /// <summary>
    /// Parses a season/episode number pair from a file name (e.g. "Show.S01E02.mkv"). Returns null if parsing fails.
    /// </summary>
    public SeasonEpisodeNumber? ParseSeasonEpisode(string fileName)
    {
        var match = SeasonEpisodePattern.Match(fileName);
        if (!match.Success)
        {
            return null;
        }

        var season = int.Parse(match.Groups["season"].Value);
        var episode = int.Parse(match.Groups["episode"].Value);
        return new SeasonEpisodeNumber(season, episode);
    }
}

