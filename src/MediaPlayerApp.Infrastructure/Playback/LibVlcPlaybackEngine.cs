// Wraps LibVLCSharp to provide play, pause, stop, and seek behavior.
using System;
using System.IO;
using LibVLCSharp.Shared;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;
using MediaPlayerApp.Domain.Exceptions;
using MediaPlayerApp.Domain.ValueObjects;

namespace MediaPlayerApp.Infrastructure.Playback;

/// <summary>
/// Implements <see cref="IMediaPlaybackEngine"/> using LibVLCSharp, driving playback through the
/// shared <see cref="LibVlcInitializer"/> engine instance.
/// </summary>
public class LibVlcPlaybackEngine : IMediaPlaybackEngine
{
    private readonly LibVlcInitializer _libVlcInitializer;
    private readonly ILogger _logger;
    private readonly MediaPlayer _mediaPlayer;
    private Media? _currentMedia;
    private bool _disposed;

    public LibVlcPlaybackEngine(LibVlcInitializer libVlcInitializer, ILogger logger)
    {
        _libVlcInitializer = libVlcInitializer ?? throw new ArgumentNullException(nameof(libVlcInitializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _mediaPlayer = new MediaPlayer(_libVlcInitializer.LibVlc);
        _mediaPlayer.TimeChanged += OnTimeChanged;
        _mediaPlayer.Paused += OnPaused;
        _mediaPlayer.Stopped += OnStopped;
        _mediaPlayer.EndReached += OnEndReached;
    }

    public event EventHandler<PlaybackPosition>? TimeChanged;

    public event EventHandler? Paused;

    public event EventHandler? Stopped;

    public event EventHandler? EndReached;

    /// <summary>
    /// Gets the underlying LibVLCSharp <see cref="MediaPlayer"/> for binding to a video surface (e.g. VideoView).
    /// </summary>
    public MediaPlayer NativePlayer => _mediaPlayer;

    public bool IsPlaying => _mediaPlayer.IsPlaying;

    public void Load(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path must be provided.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new MediaPlaybackException($"Media file not found: {filePath}");
        }

        try
        {
            _logger.Info($"Loading media '{filePath}'.");

            DisposeCurrentMedia();

            _currentMedia = new Media(_libVlcInitializer.LibVlc, new Uri(filePath));
            _mediaPlayer.Media = _currentMedia;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load media '{filePath}'.", ex);
            throw new MediaPlaybackException($"Failed to load media '{filePath}'.", ex);
        }
    }

    public void Play()
    {
        EnsureMediaLoaded();

        try
        {
            _mediaPlayer.Play();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to start playback.", ex);
            throw new MediaPlaybackException("Failed to start playback.", ex);
        }
    }

    public void Pause()
    {
        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
        }
    }

    public void Stop()
    {
        _mediaPlayer.Stop();
    }

    public void SeekTo(TimeSpan position)
    {
        EnsureMediaLoaded();

        try
        {
            _mediaPlayer.Time = (long)position.TotalMilliseconds;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to seek to position {position}.", ex);
            throw new MediaPlaybackException($"Failed to seek to position {position}.", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _mediaPlayer.TimeChanged -= OnTimeChanged;
        _mediaPlayer.Paused -= OnPaused;
        _mediaPlayer.Stopped -= OnStopped;
        _mediaPlayer.EndReached -= OnEndReached;

        DisposeCurrentMedia();
        _mediaPlayer.Dispose();
    }

    private void EnsureMediaLoaded()
    {
        if (_mediaPlayer.Media is null)
        {
            throw new MediaPlaybackException("No media has been loaded.");
        }
    }

    private void OnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
    {
        var durationMs = _mediaPlayer.Media?.Duration ?? 0;
        TimeChanged?.Invoke(this, new PlaybackPosition(TimeSpan.FromMilliseconds(e.Time), TimeSpan.FromMilliseconds(durationMs)));
    }

    private void OnPaused(object? sender, EventArgs e) => Paused?.Invoke(this, EventArgs.Empty);

    private void OnStopped(object? sender, EventArgs e) => Stopped?.Invoke(this, EventArgs.Empty);

    private void OnEndReached(object? sender, EventArgs e) => EndReached?.Invoke(this, EventArgs.Empty);

    private void DisposeCurrentMedia()
    {
        if (_currentMedia is not null)
        {
            _currentMedia.Dispose();
            _currentMedia = null;
        }
    }
}
