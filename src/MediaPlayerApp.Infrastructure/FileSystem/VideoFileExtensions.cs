// Identifies supported video file extensions.
using System;
using System.Collections.Generic;
using System.IO;

namespace MediaPlayerApp.Infrastructure.FileSystem;

public static class VideoFileExtensions
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".m4v", ".webm", ".flv"
    };

    public static bool IsSupported(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return !string.IsNullOrEmpty(extension) && SupportedExtensions.Contains(extension);
    }
}
