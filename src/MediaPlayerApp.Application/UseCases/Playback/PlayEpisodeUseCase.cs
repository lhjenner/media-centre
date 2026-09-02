// Starts playback of a selected episode.
using System;
using MediaPlayerApp.Application.UseCases.Progress;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;
using MediaPlayerApp.Domain.Entities;
using MediaPlayerApp.Domain.Enums;
using MediaPlayerApp.Domain.Exceptions;
using MediaPlayerApp.Domain.ValueObjects;

namespace MediaPlayerApp.Application.UseCases.Playback;

/// <summary>
/// Starts playback of a selected episode: loads the media into the playback engine, resumes from
/// any stored progress position, and wires playback events to <see cref="UpdateProgressUseCase"/>
/// so progress is tracked automatically during the session.
/// </summary>
public class PlayEpisodeUseCase
{
    private readonly ILibraryRepository _libraryRepository;
    private readonly IProgressRepository _progressRepository;
    private readonly IMediaPlaybackEngine _playbackEngine;
    private readonly UpdateProgressUseCase _updateProgressUseCase;
    private readonly ILogger _logger;

    private Episode? _currentEpisode;
    private PlaybackPosition? _lastKnownPosition;

    public PlayEpisodeUseCase(
        ILibraryRepository libraryRepository,
        IProgressRepository progressRepository,
        IMediaPlaybackEngine playbackEngine,
        UpdateProgressUseCase updateProgressUseCase,
        ILogger logger)
    {
        _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
        _progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
        _playbackEngine = playbackEngine ?? throw new ArgumentNullException(nameof(playbackEngine));
        _updateProgressUseCase = updateProgressUseCase ?? throw new ArgumentNullException(nameof(updateProgressUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads and starts playback of the episode matching the given key, resuming from stored
    /// progress if available, and subscribes to playback events for automatic progress tracking.
    /// </summary>
    public void Execute(EpisodeKey episodeKey)
    {
        if (episodeKey is null)
        {
            throw new ArgumentNullException(nameof(episodeKey));
        }

        var episode = _libraryRepository.FindEpisode(episodeKey)
            ?? throw new MediaPlaybackException($"Episode '{episodeKey}' was not found in the library.");

        _currentEpisode = episode;
        _lastKnownPosition = null;

        var progress = _progressRepository.GetProgress(episodeKey);

        _logger.Info($"Starting playback of episode '{episodeKey}'.");

        _playbackEngine.Load(episode.FilePath);

        if (progress is not null && progress.Status != WatchStatus.Completed)
        {
            _playbackEngine.SeekTo(progress.Position.Position);
        }

        SubscribeToPlaybackEvents();

        _playbackEngine.Play();
    }

    private void SubscribeToPlaybackEvents()
    {
        _playbackEngine.TimeChanged -= OnTimeChanged;
        _playbackEngine.Paused -= OnPausedOrStopped;
        _playbackEngine.Stopped -= OnPausedOrStopped;
        _playbackEngine.EndReached -= OnPausedOrStopped;

        _playbackEngine.TimeChanged += OnTimeChanged;
        _playbackEngine.Paused += OnPausedOrStopped;
        _playbackEngine.Stopped += OnPausedOrStopped;
        _playbackEngine.EndReached += OnPausedOrStopped;
    }

    private void OnTimeChanged(object? sender, PlaybackPosition position)
    {
        _lastKnownPosition = position;

        if (_currentEpisode is null)
        {
            return;
        }

        _updateProgressUseCase.Execute(_currentEpisode, position, flushImmediately: false);
    }

    private void OnPausedOrStopped(object? sender, EventArgs e)
    {
        if (_currentEpisode is null || _lastKnownPosition is null)
        {
            return;
        }

        _updateProgressUseCase.Execute(_currentEpisode, _lastKnownPosition, flushImmediately: true);
    }
}

