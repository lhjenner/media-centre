// Fetches show and episode metadata from TMDB.
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;
using MediaPlayerApp.Domain.Exceptions;

namespace MediaPlayerApp.Infrastructure.Metadata;

/// <summary>
/// Implements <see cref="IMetadataProvider"/> by fetching show metadata from the TMDB API,
/// caching successful responses on disk via <see cref="MetadataCacheStore"/> to avoid
/// redundant network calls.
/// </summary>
public class TmdbMetadataProvider : IMetadataProvider
{
    private const string BaseUrl = "https://api.themoviedb.org/3";
    private const string PosterBaseUrl = "https://image.tmdb.org/t/p/w500";

    private readonly HttpClient _httpClient;
    private readonly IAppConfiguration _configuration;
    private readonly MetadataCacheStore _cacheStore;
    private readonly ILogger _logger;

    public TmdbMetadataProvider(
        HttpClient httpClient,
        IAppConfiguration configuration,
        MetadataCacheStore cacheStore,
        ILogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// TMDB does not resolve posters from local folders; this provider only supports online lookups.
    /// </summary>
    public string? GetShowPosterPath(string showFolderPath) => null;

    /// <summary>
    /// TMDB does not resolve posters from local folders; this provider only supports online lookups.
    /// </summary>
    public string? GetSeasonPosterPath(string seasonFolderPath) => null;

    public async Task<ShowMetadataResult?> GetShowMetadataAsync(string showName)
    {
        if (string.IsNullOrWhiteSpace(showName))
        {
            throw new ArgumentException("Show name must be provided.", nameof(showName));
        }

        if (!_configuration.EnableOnlineMetadata || string.IsNullOrWhiteSpace(_configuration.TmdbApiKey))
        {
            _logger.Info("Online metadata lookups are disabled or no TMDB API key is configured.");
            return null;
        }

        if (_cacheStore.TryGet(showName, out var cachedJson) && cachedJson is not null)
        {
            _logger.Info($"Using cached TMDB metadata for '{showName}'.");
            return ParseShowMetadata(cachedJson);
        }

        return await FetchAndCacheShowMetadataAsync(showName);
    }

    private async Task<ShowMetadataResult?> FetchAndCacheShowMetadataAsync(string showName)
    {
        try
        {
            _logger.Info($"Fetching TMDB metadata for '{showName}'.");

            var searchUrl = $"{BaseUrl}/search/tv?api_key={_configuration.TmdbApiKey}&query={Uri.EscapeDataString(showName)}";
            var responseJson = await _httpClient.GetStringAsync(searchUrl);

            _cacheStore.Save(showName, responseJson);
            return ParseShowMetadata(responseJson);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to fetch TMDB metadata for '{showName}'.", ex);
            throw new MetadataRetrievalException($"Failed to fetch TMDB metadata for '{showName}'.", ex);
        }
    }

    private ShowMetadataResult? ParseShowMetadata(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
        {
            return null;
        }

        var firstResult = results[0];
        var overview = firstResult.TryGetProperty("overview", out var overviewElement)
            ? overviewElement.GetString() ?? string.Empty
            : string.Empty;

        string? posterUrl = null;
        if (firstResult.TryGetProperty("poster_path", out var posterPathElement))
        {
            var posterPath = posterPathElement.GetString();
            if (!string.IsNullOrEmpty(posterPath))
            {
                posterUrl = $"{PosterBaseUrl}{posterPath}";
            }
        }

        return new ShowMetadataResult(overview, posterUrl);
    }
}

