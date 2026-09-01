// Contract exposing validated application configuration values.
namespace MediaPlayerApp.Domain.Abstractions;

public interface IAppConfiguration
{
    string VideosRootPath { get; }

    bool EnableOnlineMetadata { get; }

    string? TmdbApiKey { get; }

    int ThumbnailTimestampSeconds { get; }

    int ContinueWatchingMaxItems { get; }

    double CompletionThresholdPercent { get; }

    int ProgressSaveDebounceSeconds { get; }

    int LogRetentionDays { get; }
}
