// Coordinates generating and caching a thumbnail for an episode.
using System;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;
using MediaPlayerApp.Domain.Exceptions;
using MediaPlayerApp.Domain.ValueObjects;

namespace MediaPlayerApp.Application.UseCases.Thumbnails;

/// <summary>
/// Coordinates resolving an episode's cached thumbnail, delegating cache-freshness checks and
/// generation to <see cref="IThumbnailCache"/>.
/// </summary>
public class GenerateEpisodeThumbnailUseCase
{
    private readonly ILibraryRepository _libraryRepository;
    private readonly IThumbnailCache _thumbnailCache;
    private readonly ILogger _logger;

    public GenerateEpisodeThumbnailUseCase(
        ILibraryRepository libraryRepository,
        IThumbnailCache thumbnailCache,
        ILogger logger)
    {
        _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
        _thumbnailCache = thumbnailCache ?? throw new ArgumentNullException(nameof(thumbnailCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns the thumbnail file path for the given episode, generating and caching it if needed.
    /// </summary>
    public string Execute(EpisodeKey episodeKey)
    {
        if (episodeKey is null)
        {
            throw new ArgumentNullException(nameof(episodeKey));
        }

        var episode = _libraryRepository.FindEpisode(episodeKey);

        if (episode is null)
        {
            _logger.Error($"Cannot generate thumbnail: episode '{episodeKey}' was not found in the library.");
            throw new ThumbnailGenerationException($"Episode '{episodeKey}' was not found in the library.");
        }

        _logger.Info($"Resolving thumbnail for episode '{episodeKey}'.");

        return _thumbnailCache.GetThumbnailPath(episode);
    }
}

