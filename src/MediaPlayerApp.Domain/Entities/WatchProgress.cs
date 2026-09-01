// Represents an episode's playback position, duration, and watch status.
using System;
using MediaPlayerApp.Domain.Enums;
using MediaPlayerApp.Domain.ValueObjects;

namespace MediaPlayerApp.Domain.Entities;

public class WatchProgress
{
    public WatchProgress(
        EpisodeKey episodeKey,
        string showName,
        int season,
        int episode,
        string filePath,
        PlaybackPosition position,
        WatchStatus status,
        DateTime lastWatchedUtc,
        string? episodeTitle = null,
        string? thumbnailPath = null)
    {
        EpisodeKey = episodeKey;
        ShowName = showName;
        Season = season;
        Episode = episode;
        FilePath = filePath;
        Position = position;
        Status = status;
        LastWatchedUtc = lastWatchedUtc;
        EpisodeTitle = episodeTitle;
        ThumbnailPath = thumbnailPath;
    }

    public EpisodeKey EpisodeKey { get; }

    public string ShowName { get; }

    public int Season { get; }

    public int Episode { get; }

    public string? EpisodeTitle { get; }

    public string FilePath { get; }

    public PlaybackPosition Position { get; }

    public WatchStatus Status { get; }

    public DateTime LastWatchedUtc { get; }

    public string? ThumbnailPath { get; }
}
