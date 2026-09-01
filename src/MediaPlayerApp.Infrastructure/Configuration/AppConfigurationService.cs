// Implements IAppConfiguration; loads, validates, and exposes settings.
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;

namespace MediaPlayerApp.Infrastructure.Configuration;

/// <summary>
/// Implements <see cref="IAppConfiguration"/> by loading/validating config.json from the app's
/// data folder, falling back to sensible defaults for any missing/invalid individual field.
/// </summary>
public class AppConfigurationService : IAppConfiguration
{
    private readonly AppPaths _appPaths;
    private readonly ILogger _logger;

    public AppConfigurationService(AppPaths appPaths, ILogger logger)
    {
        _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Load();
    }

    public string VideosRootPath { get; private set; } = string.Empty;

    public bool EnableOnlineMetadata { get; private set; }

    public string? TmdbApiKey { get; private set; }

    public int ThumbnailTimestampSeconds { get; private set; } = 30;

    public int ContinueWatchingMaxItems { get; private set; } = 20;

    public double CompletionThresholdPercent { get; private set; } = 95;

    public int ProgressSaveDebounceSeconds { get; private set; } = 5;

    public int LogRetentionDays { get; private set; } = 14;

    private void Load()
    {
        var model = ReadConfigFile() ?? new ConfigFileModel();

        VideosRootPath = model.VideosRootPath ?? string.Empty;
        EnableOnlineMetadata = model.EnableOnlineMetadata;
        TmdbApiKey = model.TmdbApiKey;
        ThumbnailTimestampSeconds = model.ThumbnailTimestampSeconds > 0 ? model.ThumbnailTimestampSeconds : 30;
        ContinueWatchingMaxItems = model.ContinueWatchingMaxItems > 0 ? model.ContinueWatchingMaxItems : 20;
        CompletionThresholdPercent = model.CompletionThresholdPercent > 0 ? model.CompletionThresholdPercent : 95;
        ProgressSaveDebounceSeconds = model.ProgressSaveDebounceSeconds > 0 ? model.ProgressSaveDebounceSeconds : 5;
        LogRetentionDays = model.LogRetentionDays > 0 ? model.LogRetentionDays : 14;
    }

    private ConfigFileModel? ReadConfigFile()
    {
        var filePath = _appPaths.ConfigFilePath;

        if (!File.Exists(filePath))
        {
            _logger.Info("No config.json found; using default configuration.");
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ConfigFileModel>(json);
        }
        catch (Exception ex)
        {
            _logger.Warning($"Config file at '{filePath}' is corrupt or unreadable: {ex.Message}. Using default configuration.");
            return null;
        }
    }

    private class ConfigFileModel
    {
        [JsonPropertyName("videosRootPath")]
        public string? VideosRootPath { get; set; }

        [JsonPropertyName("enableOnlineMetadata")]
        public bool EnableOnlineMetadata { get; set; }

        [JsonPropertyName("tmdbApiKey")]
        public string? TmdbApiKey { get; set; }

        [JsonPropertyName("thumbnailTimestampSeconds")]
        public int ThumbnailTimestampSeconds { get; set; }

        [JsonPropertyName("continueWatchingMaxItems")]
        public int ContinueWatchingMaxItems { get; set; }

        [JsonPropertyName("completionThresholdPercent")]
        public double CompletionThresholdPercent { get; set; }

        [JsonPropertyName("progressSaveDebounceSeconds")]
        public int ProgressSaveDebounceSeconds { get; set; }

        [JsonPropertyName("logRetentionDays")]
        public int LogRetentionDays { get; set; }
    }
}

