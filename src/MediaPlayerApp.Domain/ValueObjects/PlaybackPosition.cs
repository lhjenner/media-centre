// Represents a playback position/duration pair with helper comparisons.
using System;

namespace MediaPlayerApp.Domain.ValueObjects;

public class PlaybackPosition
{
    public PlaybackPosition(TimeSpan position, TimeSpan duration)
    {
        Position = position;
        Duration = duration;
    }

    public TimeSpan Position { get; }

    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets the watched fraction (0.0 to 1.0), or 0 if the duration is unknown.
    /// </summary>
    public double Fraction => Duration > TimeSpan.Zero
        ? Math.Clamp(Position.TotalMilliseconds / Duration.TotalMilliseconds, 0, 1)
        : 0;

    /// <summary>
    /// Determines whether the position has reached or passed the given completion threshold.
    /// </summary>
    public bool HasReached(TimeSpan threshold) => Position >= threshold;
}
