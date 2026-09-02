// Orchestrates scanning the root Videos folder into a show/season/episode library.
using System;
using System.Collections.Generic;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;
using MediaPlayerApp.Domain.Entities;
using MediaPlayerApp.Domain.Exceptions;

namespace MediaPlayerApp.Application.UseCases.LibraryScanning;

/// <summary>
/// Orchestrates a full scan of the configured Videos root folder, delegating the actual
/// filesystem traversal to <see cref="ILibraryRepository"/> and returning the resulting library.
/// </summary>
public class ScanLibraryUseCase
{
    private readonly ILibraryRepository _libraryRepository;
    private readonly IAppConfiguration _configuration;
    private readonly ILogger _logger;

    public ScanLibraryUseCase(ILibraryRepository libraryRepository, IAppConfiguration configuration, ILogger logger)
    {
        _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Scans the configured Videos root folder and returns the resulting show/season/episode library.
    /// </summary>
    public IReadOnlyList<Show> Execute()
    {
        var videosRootPath = _configuration.VideosRootPath;

        if (string.IsNullOrWhiteSpace(videosRootPath))
        {
            _logger.Error("Cannot scan library: no Videos root path is configured.");
            throw new LibraryScanException("No Videos root path is configured.");
        }

        try
        {
            _logger.Info($"Starting library scan of '{videosRootPath}'.");

            var library = _libraryRepository.ScanLibrary(videosRootPath);

            _logger.Info($"Library scan complete. Found {library.Count} show(s).");
            return library;
        }
        catch (LibraryScanException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Library scan failed for '{videosRootPath}'.", ex);
            throw new LibraryScanException($"Library scan failed for '{videosRootPath}'.", ex);
        }
    }
}

