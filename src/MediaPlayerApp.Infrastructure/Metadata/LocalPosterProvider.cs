// Resolves poster images from local show/season folders.
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;

namespace MediaPlayerApp.Infrastructure.Metadata;

/// <summary>
/// Implements <see cref="IMetadataProvider"/> by looking for well-known poster/artwork file names
/// directly inside a show or season folder on disk.
/// </summary>
public class LocalPosterProvider : IMetadataProvider
{
    private static readonly string[] CandidateFileNames =
    {
        "poster.jpg", "poster.jpeg", "poster.png",
        "folder.jpg", "folder.jpeg", "folder.png",
        "cover.jpg", "cover.jpeg", "cover.png"
    };

    private readonly ILogger _logger;

    public LocalPosterProvider(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string? GetShowPosterPath(string showFolderPath) => FindPoster(showFolderPath);

    public string? GetSeasonPosterPath(string seasonFolderPath) => FindPoster(seasonFolderPath);

    /// <summary>
    /// This provider only resolves posters from local folders and does not perform online lookups.
    /// </summary>
    public Task<ShowMetadataResult?> GetShowMetadataAsync(string showName) => Task.FromResult<ShowMetadataResult?>(null);

    private string? FindPoster(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            _logger.Warning($"Cannot resolve local poster; folder does not exist: '{folderPath}'.");
            return null;
        }

        var match = CandidateFileNames
            .Select(fileName => Path.Combine(folderPath, fileName))
            .FirstOrDefault(File.Exists);

        if (match is null)
        {
            _logger.Info($"No local poster found in folder '{folderPath}'.");
        }

        return match;
    }
}
