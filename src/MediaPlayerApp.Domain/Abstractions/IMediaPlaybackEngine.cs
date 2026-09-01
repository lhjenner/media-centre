// Contract for controlling media playback.
using System;
using MediaPlayerApp.Domain.ValueObjects;

namespace MediaPlayerApp.Domain.Abstractions;

public interface IMediaPlaybackEngine : IDisposable
{
    /// <summary>
    /// Raised whenever the current playback position changes.
    /// </summary>
    event EventHandler<PlaybackPosition>? TimeChanged;

    /// <summary>
    /// Raised when playback is paused.
    /// </summary>
    event EventHandler? Paused;

    /// <summary>
    /// Raised when playback is stopped.
    /// </summary>
    event EventHandler? Stopped;

    /// <summary>
    /// Raised when playback reaches the end of the media.
    /// </summary>
    event EventHandler? EndReached;

    /// <summary>
    /// Gets a value indicating whether media is currently playing.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Loads the media file located at the given path, ready for playback.
    /// </summary>
    void Load(string filePath);

    /// <summary>
    /// Starts or resumes playback of the currently loaded media.
    /// </summary>
    void Play();

    /// <summary>
    /// Pauses playback of the currently loaded media.
    /// </summary>
    void Pause();

    /// <summary>
    /// Stops playback of the currently loaded media.
    /// </summary>
    void Stop();

    /// <summary>
    /// Seeks to the given position within the currently loaded media.
    /// </summary>
    void SeekTo(TimeSpan position);
}
