// Resolves application data, cache, and thumbnail folder paths.
using System;
using System.IO;

namespace MediaPlayerApp.Infrastructure.Configuration;

public class AppPaths
{
    private const string AppFolderName = "MediaPlayerApp";

    public AppPaths()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        DataFolder = Path.Combine(localAppData, AppFolderName, "data");
        CacheFolder = Path.Combine(DataFolder, "cache");
        ThumbnailsFolder = Path.Combine(DataFolder, "thumbnails");
        LogsFolder = Path.Combine(DataFolder, "logs");

        EnsureFoldersExist();
    }

    public string DataFolder { get; }

    public string CacheFolder { get; }

    public string ThumbnailsFolder { get; }

    public string LogsFolder { get; }

    public string ConfigFilePath => Path.Combine(DataFolder, "config.json");

    public string ProgressFilePath => Path.Combine(DataFolder, "progress.json");

    public string MetadataCacheFilePath => Path.Combine(CacheFolder, "metadata-cache.json");

    public string PlaceholderThumbnailPath => Path.Combine(ThumbnailsFolder, "placeholder.jpg");

    private void EnsureFoldersExist()
    {
        Directory.CreateDirectory(DataFolder);
        Directory.CreateDirectory(CacheFolder);
        Directory.CreateDirectory(ThumbnailsFolder);
        Directory.CreateDirectory(LogsFolder);
    }
}
