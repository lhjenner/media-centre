// Represents a parsed season/episode number pair.
namespace MediaPlayerApp.Domain.ValueObjects;

public class SeasonEpisodeNumber
{
    public SeasonEpisodeNumber(int season, int episode)
    {
        Season = season;
        Episode = episode;
    }

    public int Season { get; }

    public int Episode { get; }
}
