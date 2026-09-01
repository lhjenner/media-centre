// Contract for loading and saving watch progress data.
using System.Collections.Generic;
using MediaPlayerApp.Domain.Entities;
using MediaPlayerApp.Domain.ValueObjects;

namespace MediaPlayerApp.Domain.Abstractions;

public interface IProgressRepository
{
    /// <summary>
    /// Loads the progress store from disk into memory. Safe to call multiple times.
    /// </summary>
    void Load();

    /// <summary>
    /// Returns the stored progress for the given episode, or null if none exists.
    /// </summary>
    WatchProgress? GetProgress(EpisodeKey episodeKey);

    /// <summary>
    /// Returns all stored progress records.
    /// </summary>
    IReadOnlyList<WatchProgress> GetAll();

    /// <summary>
    /// Returns the ordered list of episode keys for the Continue Watching row (most recent first).
    /// </summary>
    IReadOnlyList<EpisodeKey> GetContinueWatching();

    /// <summary>
    /// Creates or updates the progress record for an episode and updates the Continue Watching list.
    /// </summary>
    void SaveProgress(WatchProgress progress);

    /// <summary>
    /// Persists the in-memory progress store to disk using an atomic write.
    /// </summary>
    void Flush();
}
