// Retrieves and caches metadata from an online provider when needed.
using System;
using System.Threading.Tasks;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;
using MediaPlayerApp.Domain.Exceptions;

namespace MediaPlayerApp.Application.UseCases.Metadata;

/// <summary>
/// Retrieves show metadata (overview/poster) from the online metadata provider, honoring the
/// `enableOnlineMetadata` configuration flag; the provider itself is responsible for caching
/// results to disk to avoid redundant network calls.
/// </summary>
public class FetchOnlineMetadataUseCase
{
    private readonly IMetadataProvider _onlineMetadataProvider;
    private readonly IAppConfiguration _configuration;
    private readonly ILogger _logger;

    public FetchOnlineMetadataUseCase(
        IMetadataProvider onlineMetadataProvider,
        IAppConfiguration configuration,
        ILogger logger)
    {
        _onlineMetadataProvider = onlineMetadataProvider ?? throw new ArgumentNullException(nameof(onlineMetadataProvider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Fetches show metadata for the given show name if online metadata is enabled, returning
    /// null if it's disabled or no result was found.
    /// </summary>
    public async Task<ShowMetadataResult?> Execute(string showName)
    {
        if (string.IsNullOrWhiteSpace(showName))
        {
            throw new ArgumentException("Show name must be provided.", nameof(showName));
        }

        if (!_configuration.EnableOnlineMetadata)
        {
            _logger.Info($"Online metadata is disabled; skipping lookup for show '{showName}'.");
            return null;
        }

        try
        {
            _logger.Info($"Fetching online metadata for show '{showName}'.");

            var result = await _onlineMetadataProvider.GetShowMetadataAsync(showName);

            if (result is null)
            {
                _logger.Warning($"No online metadata found for show '{showName}'.");
            }

            return result;
        }
        catch (MetadataRetrievalException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to fetch online metadata for show '{showName}'.", ex);
            throw new MetadataRetrievalException($"Failed to fetch online metadata for show '{showName}'.", ex);
        }
    }
}

