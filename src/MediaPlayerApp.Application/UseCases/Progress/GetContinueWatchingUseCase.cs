// Builds the ordered Continue Watching list from stored progress.
using System;
using System.Collections.Generic;
using System.Linq;
using MediaPlayerApp.Application.DTOs;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;
using MediaPlayerApp.Domain.Enums;

namespace MediaPlayerApp.Application.UseCases.Progress;

/// <summary>
/// Builds the ordered Continue Watching list from stored progress, per
/// docs/06_implementation_complete.md's "Continue Watching Generation" workflow: filter to
/// in-progress episodes, sort by most recently watched, and cap at the configured maximum.
/// </summary>
public class GetContinueWatchingUseCase
{
    private readonly IProgressRepository _progressRepository;
    private readonly ILibraryRepository _libraryRepository;
    private readonly IThumbnailCache _thumbnailCache;
    private readonly IAppConfiguration _configuration;
    private readonly ILogger _logger;

    public GetContinueWatchingUseCase(
        IProgressRepository progressRepository,
        ILibraryRepository libraryRepository,
        IThumbnailCache thumbnailCache,
        IAppConfiguration configuration,
        ILogger logger)
    {
        _progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
        _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
        _thumbnailCache = thumbnailCache ?? throw new ArgumentNullException(nameof(thumbnailCache));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns the ordered Continue Watching list, most recently watched first.
    /// </summary>
    public IReadOnlyList<ContinueWatchingItemDto> Execute()
    {
        var allProgress = _progressRepository.GetAll();

        var items = allProgress
            .Where(progress => progress.Status == WatchStatus.InProgress)
            .OrderByDescending(progress => progress.LastWatchedUtc)
            .Take(_configuration.ContinueWatchingMaxItems)
            .Select(ToDto)
            .Where(dto => dto is not null)
            .Select(dto => dto!)
            .ToList();

        _logger.Info($"Built Continue Watching list with {items.Count} item(s).");

        return items;
    }

    private ContinueWatchingItemDto? ToDto(Domain.Entities.WatchProgress progress)
    {
        var episode = _libraryRepository.FindEpisode(progress.EpisodeKey);

        if (episode is null)
        {
            _logger.Warning($"Skipping Continue Watching entry for '{progress.EpisodeKey}': episode no longer found in library.");
            return null;
        }

        var thumbnailPath = _thumbnailCache.GetThumbnailPath(episode);

        return new ContinueWatchingItemDto(
            progress.EpisodeKey,
            progress.ShowName,
            progress.Season,
            progress.Episode,
            progress.EpisodeTitle,
            thumbnailPath,
            progress.Position.Fraction);
    }
}
