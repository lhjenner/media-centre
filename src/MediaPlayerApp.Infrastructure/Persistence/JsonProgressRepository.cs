// Reads and writes playback progress to a JSON file.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;
using MediaPlayerApp.Domain.Entities;
using MediaPlayerApp.Domain.Enums;
using MediaPlayerApp.Domain.Exceptions;
using MediaPlayerApp.Domain.ValueObjects;
using MediaPlayerApp.Infrastructure.Configuration;

namespace MediaPlayerApp.Infrastructure.Persistence;

/// <summary>
/// Implements <see cref="IProgressRepository"/> by persisting watch progress to a JSON file
/// (see docs/PROGRESS_SCHEMA.md), using an in-memory cache backed by atomic writes.
/// </summary>
public class JsonProgressRepository : IProgressRepository
{
    private const int CurrentSchemaVersion = 1;
    private const int ContinueWatchingMaxItems = 20;

    private readonly AppPaths _appPaths;
    private readonly AtomicFileWriter _atomicFileWriter;
    private readonly ILogger _logger;
    private readonly object _storeLock = new();

    private Dictionary<string, WatchProgress> _episodes = new(StringComparer.OrdinalIgnoreCase);
    private List<string> _continueWatching = new();
    private bool _loaded;

    public JsonProgressRepository(AppPaths appPaths, AtomicFileWriter atomicFileWriter, ILogger logger)
    {
        _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
        _atomicFileWriter = atomicFileWriter ?? throw new ArgumentNullException(nameof(atomicFileWriter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Load()
    {
        lock (_storeLock)
        {
            if (_loaded)
            {
                return;
            }

            LoadFromDisk();
            _loaded = true;
        }
    }

    public WatchProgress? GetProgress(EpisodeKey episodeKey)
    {
        if (episodeKey is null)
        {
            throw new ArgumentNullException(nameof(episodeKey));
        }

        EnsureLoaded();

        lock (_storeLock)
        {
            return _episodes.TryGetValue(episodeKey.ToString(), out var progress) ? progress : null;
        }
    }

    public IReadOnlyList<WatchProgress> GetAll()
    {
        EnsureLoaded();

        lock (_storeLock)
        {
            return _episodes.Values.ToList();
        }
    }

    public IReadOnlyList<EpisodeKey> GetContinueWatching()
    {
        EnsureLoaded();

        lock (_storeLock)
        {
            return _continueWatching
                .Where(_episodes.ContainsKey)
                .Select(key => _episodes[key].EpisodeKey)
                .ToList();
        }
    }

    public void SaveProgress(WatchProgress progress)
    {
        if (progress is null)
        {
            throw new ArgumentNullException(nameof(progress));
        }

        EnsureLoaded();

        lock (_storeLock)
        {
            var key = progress.EpisodeKey.ToString();
            _episodes[key] = progress;
            UpdateContinueWatching(key, progress.Status);
        }
    }

    public void Flush()
    {
        lock (_storeLock)
        {
            SaveToDisk();
        }
    }

    private void EnsureLoaded()
    {
        if (!_loaded)
        {
            Load();
        }
    }

    private void UpdateContinueWatching(string episodeKey, WatchStatus status)
    {
        _continueWatching.Remove(episodeKey);

        if (status is WatchStatus.InProgress)
        {
            _continueWatching.Insert(0, episodeKey);

            if (_continueWatching.Count > ContinueWatchingMaxItems)
            {
                _continueWatching.RemoveRange(ContinueWatchingMaxItems, _continueWatching.Count - ContinueWatchingMaxItems);
            }
        }
    }

    private void LoadFromDisk()
    {
        var filePath = _appPaths.ProgressFilePath;

        if (!File.Exists(filePath))
        {
            _logger.Info("No progress file found; starting with an empty progress store.");
            _episodes = new Dictionary<string, WatchProgress>(StringComparer.OrdinalIgnoreCase);
            _continueWatching = new List<string>();
            return;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var document = JsonSerializer.Deserialize<ProgressFileModel>(json)
                ?? throw new ProgressPersistenceException("Progress file deserialized to null.");

            _episodes = document.Episodes
                .ToDictionary(pair => pair.Key, pair => ToWatchProgress(pair.Key, pair.Value), StringComparer.OrdinalIgnoreCase);
            _continueWatching = document.ContinueWatching.ToList();
        }
        catch (Exception ex)
        {
            _logger.Warning($"Progress file at '{filePath}' is corrupt or unreadable: {ex.Message}. Starting with an empty store.");
            BackupCorruptFile(filePath);

            _episodes = new Dictionary<string, WatchProgress>(StringComparer.OrdinalIgnoreCase);
            _continueWatching = new List<string>();
        }
    }

    private void BackupCorruptFile(string filePath)
    {
        try
        {
            var backupPath = filePath + ".bak";
            File.Copy(filePath, backupPath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to back up corrupt progress file: {ex.Message}");
        }
    }

    private void SaveToDisk()
    {
        try
        {
            var model = new ProgressFileModel
            {
                SchemaVersion = CurrentSchemaVersion,
                LastUpdatedUtc = DateTime.UtcNow,
                Episodes = _episodes.ToDictionary(pair => pair.Key, pair => ToEpisodeProgressModel(pair.Value), StringComparer.OrdinalIgnoreCase),
                ContinueWatching = _continueWatching.ToList()
            };

            var json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
            _atomicFileWriter.WriteAllText(_appPaths.ProgressFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to write progress file.", ex);
            throw new ProgressPersistenceException("Failed to write progress file.", ex);
        }
    }

    private static WatchProgress ToWatchProgress(string episodeKeyString, EpisodeProgressModel model)
    {
        var episodeKey = new EpisodeKey(model.ShowName, model.Season, model.Episode);
        var position = new PlaybackPosition(
            TimeSpan.FromTicks(model.PositionTicks),
            TimeSpan.FromTicks(model.DurationTicks ?? 0));

        return new WatchProgress(
            episodeKey,
            model.ShowName,
            model.Season,
            model.Episode,
            model.FilePath,
            position,
            Enum.Parse<WatchStatus>(model.Status),
            model.LastWatchedUtc,
            model.EpisodeTitle,
            model.ThumbnailPath);
    }

    private static EpisodeProgressModel ToEpisodeProgressModel(WatchProgress progress) => new()
    {
        ShowName = progress.ShowName,
        Season = progress.Season,
        Episode = progress.Episode,
        EpisodeTitle = progress.EpisodeTitle,
        FilePath = progress.FilePath,
        PositionTicks = progress.Position.Position.Ticks,
        DurationTicks = progress.Position.Duration.Ticks,
        Status = progress.Status.ToString(),
        LastWatchedUtc = progress.LastWatchedUtc,
        ThumbnailPath = progress.ThumbnailPath
    };

    private class ProgressFileModel
    {
        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; }

        [JsonPropertyName("lastUpdatedUtc")]
        public DateTime LastUpdatedUtc { get; set; }

        [JsonPropertyName("episodes")]
        public Dictionary<string, EpisodeProgressModel> Episodes { get; set; } = new();

        [JsonPropertyName("continueWatching")]
        public List<string> ContinueWatching { get; set; } = new();
    }

    private class EpisodeProgressModel
    {
        [JsonPropertyName("showName")]
        public string ShowName { get; set; } = string.Empty;

        [JsonPropertyName("season")]
        public int Season { get; set; }

        [JsonPropertyName("episode")]
        public int Episode { get; set; }

        [JsonPropertyName("episodeTitle")]
        public string? EpisodeTitle { get; set; }

        [JsonPropertyName("filePath")]
        public string FilePath { get; set; } = string.Empty;

        [JsonPropertyName("positionTicks")]
        public long PositionTicks { get; set; }

        [JsonPropertyName("durationTicks")]
        public long? DurationTicks { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = nameof(WatchStatus.NotStarted);

        [JsonPropertyName("lastWatchedUtc")]
        public DateTime LastWatchedUtc { get; set; }

        [JsonPropertyName("thumbnailPath")]
        public string? ThumbnailPath { get; set; }
    }
}

