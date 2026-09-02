// Determines the poster image to use for a show or season.
using System;
using System.Threading.Tasks;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;

namespace MediaPlayerApp.Application.UseCases.Metadata;

/// <summary>
/// Determines the poster image to use for a show, preferring a local poster file and falling
/// back to an online (TMDB) lookup when no local poster is found, per
/// docs/06_implementation_complete.md section 3 ("local posters are preferred; TMDB is a fallback").
/// </summary>
public class ResolvePosterUseCase
{
    private readonly IMetadataProvider _localPosterProvider;
    private readonly IMetadataProvider _onlineMetadataProvider;
    private readonly ILogger _logger;

    public ResolvePosterUseCase(
        IMetadataProvider localPosterProvider,
        IMetadataProvider onlineMetadataProvider,
        ILogger logger)
    {
        _localPosterProvider = localPosterProvider ?? throw new ArgumentNullException(nameof(localPosterProvider));
        _onlineMetadataProvider = onlineMetadataProvider ?? throw new ArgumentNullException(nameof(onlineMetadataProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Resolves the poster to use for a show: a local file path if found in the show's folder,
    /// otherwise an online poster URL, or null if neither source has a poster.
    /// </summary>
    public async Task<string?> ExecuteForShowAsync(string showName, string showFolderPath)
    {
        if (string.IsNullOrWhiteSpace(showFolderPath))
        {
            throw new ArgumentException("Show folder path must be provided.", nameof(showFolderPath));
        }

        var localPosterPath = _localPosterProvider.GetShowPosterPath(showFolderPath);

        if (localPosterPath is not null)
        {
            _logger.Info($"Using local poster for show '{showName}'.");
            return localPosterPath;
        }

        _logger.Info($"No local poster found for show '{showName}'; falling back to online metadata.");

        var onlineMetadata = await _onlineMetadataProvider.GetShowMetadataAsync(showName);

        if (onlineMetadata?.PosterUrl is null)
        {
            _logger.Warning($"No poster available for show '{showName}' from local or online sources.");
            return null;
        }

        return onlineMetadata.PosterUrl;
    }

    /// <summary>
    /// Resolves the poster to use for a season, using only local season folder artwork
    /// (seasons do not have a dedicated online poster lookup).
    /// </summary>
    public string? ExecuteForSeason(string seasonFolderPath)
    {
        if (string.IsNullOrWhiteSpace(seasonFolderPath))
        {
            throw new ArgumentException("Season folder path must be provided.", nameof(seasonFolderPath));
        }

        return _localPosterProvider.GetSeasonPosterPath(seasonFolderPath);
    }
}

