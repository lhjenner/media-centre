// Stable identifier derived from show, season, and episode number.
using System;

namespace MediaPlayerApp.Domain.ValueObjects;

public class EpisodeKey : IEquatable<EpisodeKey>
{
    public EpisodeKey(string showName, int season, int episode)
    {
        ShowName = showName ?? throw new ArgumentNullException(nameof(showName));
        Season = season;
        Episode = episode;
    }

    public string ShowName { get; }

    public int Season { get; }

    public int Episode { get; }

    public override string ToString() => $"{ShowName}_S{Season:D2}E{Episode:D2}";

    public bool Equals(EpisodeKey? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(ShowName, other.ShowName, StringComparison.OrdinalIgnoreCase)
            && Season == other.Season
            && Episode == other.Episode;
    }

    public override bool Equals(object? obj) => Equals(obj as EpisodeKey);

    public override int GetHashCode() => HashCode.Combine(ShowName.ToUpperInvariant(), Season, Episode);
}
