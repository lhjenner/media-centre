// Contract for retrieving the scanned show/season/episode library.
using System.Collections.Generic;
using MediaPlayerApp.Domain.Entities;
using MediaPlayerApp.Domain.ValueObjects;

namespace MediaPlayerApp.Domain.Abstractions;

public interface ILibraryRepository
{
    /// <summary>
    /// Scans the given Videos root folder and builds the show/season/episode library.
    /// </summary>
    IReadOnlyList<Show> ScanLibrary(string videosRootPath);

    /// <summary>
    /// Returns the most recently scanned library, or an empty list if scanning has not yet occurred.
    /// </summary>
    IReadOnlyList<Show> GetLibrary();

    /// <summary>
    /// Finds the episode matching the given key, or null if not found.
    /// </summary>
    Episode? FindEpisode(EpisodeKey episodeKey);
}
