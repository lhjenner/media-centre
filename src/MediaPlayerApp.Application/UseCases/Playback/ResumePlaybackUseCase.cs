// Resumes playback of an episode from its stored progress position.
using System;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;
using MediaPlayerApp.Domain.Exceptions;
using MediaPlayerApp.Domain.ValueObjects;

namespace MediaPlayerApp.Application.UseCases.Playback;

/// <summary>
/// Resumes playback of an episode (e.g., from the Continue Watching row) by requiring that a
/// stored progress record exists, then delegating to <see cref="PlayEpisodeUseCase"/>, which
/// handles loading the media, seeking to the stored position, and wiring progress tracking.
/// </summary>
public class ResumePlaybackUseCase
{
    private readonly IProgressRepository _progressRepository;
    private readonly PlayEpisodeUseCase _playEpisodeUseCase;
    private readonly ILogger _logger;

    public ResumePlaybackUseCase(
        IProgressRepository progressRepository,
        PlayEpisodeUseCase playEpisodeUseCase,
        ILogger logger)
    {
        _progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
        _playEpisodeUseCase = playEpisodeUseCase ?? throw new ArgumentNullException(nameof(playEpisodeUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Resumes playback of the episode matching the given key from its stored progress position.
    /// </summary>
    public void Execute(EpisodeKey episodeKey)
    {
        if (episodeKey is null)
        {
            throw new ArgumentNullException(nameof(episodeKey));
        }

        var progress = _progressRepository.GetProgress(episodeKey)
            ?? throw new MediaPlaybackException($"No stored progress found to resume for episode '{episodeKey}'.");

        _logger.Info($"Resuming playback of episode '{episodeKey}' from {progress.Position.Position}.");

        _playEpisodeUseCase.Execute(episodeKey);
    }
}

