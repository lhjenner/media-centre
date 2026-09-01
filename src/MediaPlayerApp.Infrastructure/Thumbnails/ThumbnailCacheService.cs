// Implements IThumbnailCache; checks disk cache, triggers generation, and returns thumbnail paths.
using System;
using System.IO;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;
using MediaPlayerApp.Domain.Entities;
using MediaPlayerApp.Infrastructure.Configuration;

namespace MediaPlayerApp.Infrastructure.Thumbnails;

/// <summary>
/// Implements <see cref="IThumbnailCache"/> by mediating between callers and the underlying
/// <see cref="IThumbnailGenerator"/>, ensuring a thumbnail is generated at most once per
/// unchanged video file, per docs/06_implementation_complete.md section 8.
/// </summary>
public class ThumbnailCacheService : IThumbnailCache
{
    private readonly IThumbnailGenerator _thumbnailGenerator;
    private readonly IAppConfiguration _configuration;
    private readonly AppPaths _appPaths;
    private readonly ILogger _logger;

    public ThumbnailCacheService(
        IThumbnailGenerator thumbnailGenerator,
        IAppConfiguration configuration,
        AppPaths appPaths,
        ILogger logger)
    {
        _thumbnailGenerator = thumbnailGenerator ?? throw new ArgumentNullException(nameof(thumbnailGenerator));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string GetThumbnailPath(Episode episode)
    {
        if (episode is null)
        {
            throw new ArgumentNullException(nameof(episode));
        }

        var cacheFilePath = GetCacheFilePath(episode);

        if (IsCacheFresh(cacheFilePath, episode.FilePath))
        {
            return cacheFilePath;
        }

        return TryGenerateThumbnail(episode, cacheFilePath)
            ? cacheFilePath
            : _appPaths.PlaceholderThumbnailPath;
    }

    private string GetCacheFilePath(Episode episode) =>
        Path.Combine(_appPaths.ThumbnailsFolder, $"{episode.Key}.jpg");

    private bool IsCacheFresh(string cacheFilePath, string videoFilePath)
    {
        if (!File.Exists(cacheFilePath))
        {
            return false;
        }

        var thumbnailWrittenUtc = File.GetLastWriteTimeUtc(cacheFilePath);
        var videoLastModifiedUtc = File.GetLastWriteTimeUtc(videoFilePath);

        return thumbnailWrittenUtc >= videoLastModifiedUtc;
    }

    private bool TryGenerateThumbnail(Episode episode, string cacheFilePath)
    {
        try
        {
            _thumbnailGenerator.GenerateThumbnail(episode.FilePath, cacheFilePath, _configuration.ThumbnailTimestampSeconds);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to generate thumbnail for episode '{episode.Key}'; using placeholder. {ex.Message}");
            return false;
        }
    }
}

