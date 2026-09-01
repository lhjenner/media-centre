// Contract for checking, storing, and retrieving cached thumbnails.
using MediaPlayerApp.Domain.Entities;

namespace MediaPlayerApp.Domain.Abstractions;

public interface IThumbnailCache
{
    /// <summary>
    /// Returns the cached thumbnail file path for the given episode, generating it first if missing
    /// or stale. Returns a placeholder image path if generation fails.
    /// </summary>
    string GetThumbnailPath(Episode episode);
}
