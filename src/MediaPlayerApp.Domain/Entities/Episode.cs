// Represents a single episode's file path, number, title, and metadata.
using MediaPlayerApp.Domain.ValueObjects;

namespace MediaPlayerApp.Domain.Entities;

public class Episode
{
    public Episode(EpisodeKey key, int episodeNumber, string filePath, string title)
    {
        Key = key;
        EpisodeNumber = episodeNumber;
        FilePath = filePath;
        Title = title;
    }

    public EpisodeKey Key { get; }

    public int EpisodeNumber { get; }

    public string FilePath { get; }

    public string Title { get; }
}
