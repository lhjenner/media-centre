// Contract for resolving posters and show/episode metadata.
using System.Threading.Tasks;

namespace MediaPlayerApp.Domain.Abstractions;

public interface IMetadataProvider
{
    /// <summary>
    /// Resolves the poster image path for a show from its local folder, or null if none is found.
    /// </summary>
    string? GetShowPosterPath(string showFolderPath);

    /// <summary>
    /// Resolves the poster image path for a season from its local folder, or null if none is found.
    /// </summary>
    string? GetSeasonPosterPath(string seasonFolderPath);

    /// <summary>
    /// Resolves show overview text and poster URL from an online provider, or null if unavailable.
    /// </summary>
    Task<ShowMetadataResult?> GetShowMetadataAsync(string showName);
}

/// <summary>
/// Lightweight result of an online show metadata lookup.
/// </summary>
public record ShowMetadataResult(string Overview, string? PosterUrl);
