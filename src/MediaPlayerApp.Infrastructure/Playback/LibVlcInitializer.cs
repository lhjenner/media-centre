// Initializes and holds the shared LibVLC engine instance.
using System;
using LibVLCSharp.Shared;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Exceptions;

namespace MediaPlayerApp.Infrastructure.Playback;

/// <summary>
/// Initializes LibVLCSharp exactly once and holds the shared <see cref="LibVLC"/> instance
/// used by all playback and thumbnail components across the application.
/// </summary>
public class LibVlcInitializer
{
    private readonly ILogger _logger;
    private readonly object _lock = new();
    private LibVLC? _libVlc;
    private bool _initialized;

    public LibVlcInitializer(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the shared LibVLC engine instance, initializing it on first access.
    /// </summary>
    public LibVLC LibVlc
    {
        get
        {
            EnsureInitialized();
            return _libVlc!;
        }
    }

    /// <summary>
    /// Ensures LibVLCSharp's native core and the shared engine instance are initialized exactly once.
    /// </summary>
    public void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                _logger.Info("Initializing LibVLC engine.");

                Core.Initialize();
                _libVlc = new LibVLC();

                _initialized = true;
                _logger.Info("LibVLC engine initialized successfully.");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to initialize LibVLC engine.", ex);
                throw new MediaPlaybackException("Failed to initialize the LibVLC engine.", ex);
            }
        }
    }
}
