// Lightweight data shape representing a Continue Watching entry.
using MediaPlayerApp.Domain.ValueObjects;

namespace MediaPlayerApp.Application.DTOs;

public class ContinueWatchingItemDto
{
    public ContinueWatchingItemDto(
        EpisodeKey episodeKey,
        string showName,
        int season,
        int episode,
        string? episodeTitle,
        string thumbnailPath,
        double watchedFraction)
    {
        EpisodeKey = episodeKey;
        ShowName = showName;
        Season = season;
        Episode = episode;
        EpisodeTitle = episodeTitle;
        ThumbnailPath = thumbnailPath;
        WatchedFraction = watchedFraction;
    }

    public EpisodeKey EpisodeKey { get; }

    public string ShowName { get; }

    public int Season { get; }

    public int Episode { get; }

    public string? EpisodeTitle { get; }

    public string ThumbnailPath { get; }

    public double WatchedFraction { get; }
}
