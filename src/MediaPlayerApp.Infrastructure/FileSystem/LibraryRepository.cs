// Scans the filesystem and builds the show/season/episode library.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaPlayerApp.Application.UseCases.LibraryScanning;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;
using MediaPlayerApp.Domain.Entities;
using MediaPlayerApp.Domain.Exceptions;
using MediaPlayerApp.Domain.ValueObjects;

namespace MediaPlayerApp.Infrastructure.FileSystem;

/// <summary>
/// Implements <see cref="ILibraryRepository"/> by scanning the Videos root folder on disk and
/// building an in-memory show/season/episode library.
/// </summary>
public class LibraryRepository : ILibraryRepository
{
    private const int DefaultSeasonNumber = 1;

    private readonly FileNameParsingService _fileNameParsingService;
    private readonly ILogger _logger;
    private readonly object _libraryLock = new();
    private IReadOnlyList<Show> _library = Array.Empty<Show>();

    public LibraryRepository(FileNameParsingService fileNameParsingService, ILogger logger)
    {
        _fileNameParsingService = fileNameParsingService ?? throw new ArgumentNullException(nameof(fileNameParsingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<Show> ScanLibrary(string videosRootPath)
    {
        if (string.IsNullOrWhiteSpace(videosRootPath) || !Directory.Exists(videosRootPath))
        {
            throw new LibraryScanException($"Videos root folder is invalid or inaccessible: {videosRootPath}");
        }

        try
        {
            _logger.Info($"Scanning library at '{videosRootPath}'.");

            var shows = Directory.GetDirectories(videosRootPath)
                .Select(BuildShow)
                .ToList();

            lock (_libraryLock)
            {
                _library = shows;
            }

            _logger.Info($"Library scan complete. Found {shows.Count} show(s).");
            return shows;
        }
        catch (LibraryScanException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to scan library at '{videosRootPath}'.", ex);
            throw new LibraryScanException($"Failed to scan library at '{videosRootPath}'.", ex);
        }
    }

    public IReadOnlyList<Show> GetLibrary()
    {
        lock (_libraryLock)
        {
            return _library;
        }
    }

    public Episode? FindEpisode(EpisodeKey episodeKey)
    {
        if (episodeKey is null)
        {
            throw new ArgumentNullException(nameof(episodeKey));
        }

        return GetLibrary()
            .Where(show => string.Equals(show.Name, episodeKey.ShowName, StringComparison.OrdinalIgnoreCase))
            .SelectMany(show => show.Seasons)
            .Where(season => season.SeasonNumber == episodeKey.Season)
            .SelectMany(season => season.Episodes)
            .FirstOrDefault(episode => episode.EpisodeNumber == episodeKey.Episode);
    }

    private Show BuildShow(string showFolderPath)
    {
        var showName = FolderNameCleaner.Clean(Path.GetFileName(showFolderPath));
        var show = new Show(showName, showFolderPath);

        var seasonFolders = Directory.GetDirectories(showFolderPath);
        var seasonSources = seasonFolders.Length > 0
            ? seasonFolders
            : new[] { showFolderPath };

        foreach (var seasonFolder in seasonSources)
        {
            var season = BuildSeason(showName, seasonFolder);
            if (season.Episodes.Count > 0)
            {
                show.Seasons.Add(season);
            }
        }

        return show;
    }

    private Season BuildSeason(string showName, string seasonFolderPath)
    {
        var seasonNumber = _fileNameParsingService.ParseSeason(Path.GetFileName(seasonFolderPath)) ?? DefaultSeasonNumber;
        var season = new Season(seasonNumber);

        var videoFiles = Directory.GetFiles(seasonFolderPath)
            .Where(VideoFileExtensions.IsSupported);

        foreach (var filePath in videoFiles)
        {
            var episode = TryBuildEpisode(showName, filePath);
            if (episode is not null)
            {
                season.Episodes.Add(episode);
            }
        }

        return season;
    }

    private Episode? TryBuildEpisode(string showName, string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var seasonEpisode = _fileNameParsingService.ParseSeasonEpisode(fileName);

        if (seasonEpisode is null)
        {
            _logger.Warning($"Unable to parse season/episode number from file '{fileName}'. Skipping.");
            return null;
        }

        var title = FolderNameCleaner.Clean(Path.GetFileNameWithoutExtension(filePath));
        var key = new EpisodeKey(showName, seasonEpisode.Season, seasonEpisode.Episode);
        return new Episode(key, seasonEpisode.Episode, filePath, title);
    }
}

