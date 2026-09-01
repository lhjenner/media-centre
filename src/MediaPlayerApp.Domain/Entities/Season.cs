// Represents a season number and its collection of episodes.
using System.Collections.Generic;

namespace MediaPlayerApp.Domain.Entities;

public class Season
{
    public Season(int seasonNumber)
    {
        SeasonNumber = seasonNumber;
    }

    public int SeasonNumber { get; }

    public List<Episode> Episodes { get; } = new();
}
