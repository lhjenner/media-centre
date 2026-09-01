// Generates episode thumbnails using LibVLCSharp frame snapshots.
using System;
using System.IO;
using System.Threading;
using LibVLCSharp.Shared;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;
using MediaPlayerApp.Domain.Exceptions;
using MediaPlayerApp.Infrastructure.Playback;

namespace MediaPlayerApp.Infrastructure.Thumbnails;

/// <summary>
/// Implements <see cref="IThumbnailGenerator"/> by seeking to a configured timestamp offset in a
/// video file and capturing a frame snapshot using a headless LibVLCSharp <see cref="MediaPlayer"/>
/// built from the shared <see cref="LibVlcInitializer"/> engine instance.
/// </summary>
public class VlcSnapshotThumbnailGenerator : IThumbnailGenerator
{
    private static readonly TimeSpan SeekTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SnapshotTimeout = TimeSpan.FromSeconds(10);

    private readonly LibVlcInitializer _libVlcInitializer;
    private readonly ILogger _logger;

    public VlcSnapshotThumbnailGenerator(LibVlcInitializer libVlcInitializer, ILogger logger)
    {
        _libVlcInitializer = libVlcInitializer ?? throw new ArgumentNullException(nameof(libVlcInitializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void GenerateThumbnail(string videoFilePath, string outputFilePath, int timestampSeconds)
    {
        if (!File.Exists(videoFilePath))
        {
            throw new ThumbnailGenerationException($"Video file not found: {videoFilePath}");
        }

        var outputDirectory = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        try
        {
            _logger.Info($"Generating thumbnail for '{videoFilePath}' at {timestampSeconds}s.");
            CaptureSnapshot(videoFilePath, outputFilePath, timestampSeconds);
            _logger.Info($"Thumbnail generated at '{outputFilePath}'.");
        }
        catch (ThumbnailGenerationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to generate thumbnail for '{videoFilePath}'.", ex);
            throw new ThumbnailGenerationException($"Failed to generate thumbnail for '{videoFilePath}'.", ex);
        }
    }

    private void CaptureSnapshot(string videoFilePath, string outputFilePath, int timestampSeconds)
    {
        using var media = new Media(_libVlcInitializer.LibVlc, new Uri(videoFilePath));
        using var mediaPlayer = new MediaPlayer(media);

        using var playingSignal = new ManualResetEventSlim(false);
        EventHandler<EventArgs>? onPlaying = (_, _) => playingSignal.Set();
        mediaPlayer.Playing += onPlaying;

        try
        {
            mediaPlayer.Play();

            if (!playingSignal.Wait(SeekTimeout))
            {
                throw new ThumbnailGenerationException($"Timed out waiting for media to start playing: {videoFilePath}");
            }

            mediaPlayer.Time = timestampSeconds * 1000L;

            using var snapshotSignal = new ManualResetEventSlim(false);
            EventHandler<MediaPlayerSnapshotTakenEventArgs>? onSnapshotTaken = (_, _) => snapshotSignal.Set();
            mediaPlayer.SnapshotTaken += onSnapshotTaken;

            try
            {
                if (!mediaPlayer.TakeSnapshot(0, outputFilePath, 0, 0))
                {
                    throw new ThumbnailGenerationException($"LibVLC failed to take a snapshot for: {videoFilePath}");
                }

                if (!snapshotSignal.Wait(SnapshotTimeout))
                {
                    throw new ThumbnailGenerationException($"Timed out waiting for snapshot to be written: {videoFilePath}");
                }
            }
            finally
            {
                mediaPlayer.SnapshotTaken -= onSnapshotTaken;
            }
        }
        finally
        {
            mediaPlayer.Playing -= onPlaying;
            mediaPlayer.Stop();
        }
    }
}

