// Updates and persists an episode's watch progress.
using System;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;
using MediaPlayerApp.Domain.Entities;
using MediaPlayerApp.Domain.Enums;
using MediaPlayerApp.Domain.Exceptions;
using MediaPlayerApp.Domain.ValueObjects;

namespace MediaPlayerApp.Application.UseCases.Progress;

/// <summary>
/// Updates the in-memory watch progress for an episode and persists it, per
/// docs/06_implementation_complete.md's "Progress Updating" workflow: debounced writes during
/// active playback, immediate flush on pause/stop/completion.
/// </summary>
public class UpdateProgressUseCase
{
    private readonly IProgressRepository _progressRepository;
    private readonly IAppConfiguration _configuration;
    private readonly ILogger _logger;

    public UpdateProgressUseCase(IProgressRepository progressRepository, IAppConfiguration configuration, ILogger logger)
    {
        _progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Updates the stored progress for the given episode, optionally flushing immediately to disk
    /// (e.g., on pause/stop/end-reached) rather than relying on the next debounced flush.
    /// </summary>
    public void Execute(Episode episode, PlaybackPosition position, bool flushImmediately)
    {
        if (episode is null)
        {
            throw new ArgumentNullException(nameof(episode));
        }

        if (position is null)
        {
            throw new ArgumentNullException(nameof(position));
        }

        try
        {
            var status = DetermineStatus(position);

            var progress = new WatchProgress(
                episode.Key,
                episode.Key.ShowName,
                episode.Key.Season,
                episode.EpisodeNumber,
                episode.FilePath,
                position,
                status,
                DateTime.UtcNow,
                episode.Title);

            _progressRepository.SaveProgress(progress);

            if (flushImmediately)
            {
                _progressRepository.Flush();
                _logger.Info($"Progress for episode '{episode.Key}' saved and flushed ({status}).");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to update progress for episode '{episode.Key}'.", ex);
            throw new ProgressPersistenceException($"Failed to update progress for episode '{episode.Key}'.", ex);
        }
    }

    private WatchStatus DetermineStatus(PlaybackPosition position)
    {
        var completionThreshold = _configuration.CompletionThresholdPercent / 100.0;

        if (position.Fraction >= completionThreshold)
        {
            return WatchStatus.Completed;
        }

        return position.Position > TimeSpan.Zero ? WatchStatus.InProgress : WatchStatus.NotStarted;
    }
}

