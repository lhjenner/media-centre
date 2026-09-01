// Represents a show with its name, folder path, and collection of seasons.
using System.Collections.Generic;

namespace MediaPlayerApp.Domain.Entities;

public class Show
{
    public Show(string name, string folderPath)
    {
        Name = name;
        FolderPath = folderPath;
    }

    public string Name { get; }

    public string FolderPath { get; }

    public List<Season> Seasons { get; } = new();
}
