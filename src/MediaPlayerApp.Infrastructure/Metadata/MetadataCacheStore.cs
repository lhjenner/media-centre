// Caches downloaded metadata and assets on disk.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Infrastructure.Configuration;
using MediaPlayerApp.Infrastructure.Persistence;

namespace MediaPlayerApp.Infrastructure.Metadata;

/// <summary>
/// Caches downloaded metadata (e.g., TMDB responses) on disk as a single JSON map file
/// (metadata-cache.json), keyed by a caller-supplied cache key (e.g., show name).
/// </summary>
public class MetadataCacheStore
{
    private readonly AppPaths _appPaths;
    private readonly AtomicFileWriter _atomicFileWriter;
    private readonly ILogger _logger;
    private readonly object _cacheLock = new();

    private Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    public MetadataCacheStore(AppPaths appPaths, AtomicFileWriter atomicFileWriter, ILogger logger)
    {
        _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
        _atomicFileWriter = atomicFileWriter ?? throw new ArgumentNullException(nameof(atomicFileWriter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads the metadata cache file from disk into memory. Safe to call multiple times.
    /// </summary>
    public void Load()
    {
        lock (_cacheLock)
        {
            if (_loaded)
            {
                return;
            }

            LoadFromDisk();
            _loaded = true;
        }
    }

    /// <summary>
    /// Attempts to retrieve a cached JSON payload for the given key.
    /// </summary>
    public bool TryGet(string key, out string? json)
    {
        Load();

        lock (_cacheLock)
        {
            return _cache.TryGetValue(key, out json);
        }
    }

    /// <summary>
    /// Stores a JSON payload for the given key and immediately flushes it to disk.
    /// </summary>
    public void Save(string key, string json)
    {
        Load();

        lock (_cacheLock)
        {
            _cache[key] = json;
            SaveToDisk();
        }
    }

    private void LoadFromDisk()
    {
        var filePath = _appPaths.MetadataCacheFilePath;

        if (!File.Exists(filePath))
        {
            _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.Warning($"Metadata cache file at '{filePath}' is corrupt or unreadable: {ex.Message}. Starting with an empty cache.");
            _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SaveToDisk()
    {
        try
        {
            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
            _atomicFileWriter.WriteAllText(_appPaths.MetadataCacheFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to write metadata cache file.", ex);
        }
    }
}
