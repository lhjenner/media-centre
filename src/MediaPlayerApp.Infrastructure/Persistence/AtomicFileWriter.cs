// Writes files safely using a temp-file-then-rename pattern.
using System;
using System.IO;

namespace MediaPlayerApp.Infrastructure.Persistence;

public class AtomicFileWriter
{
    /// <summary>
    /// Writes the given text content to <paramref name="targetFilePath"/> atomically, by writing to a
    /// temporary file first and then replacing the target file, to avoid partial/corrupt writes.
    /// </summary>
    public void WriteAllText(string targetFilePath, string content)
    {
        if (string.IsNullOrWhiteSpace(targetFilePath))
        {
            throw new ArgumentException("Target file path must be provided.", nameof(targetFilePath));
        }

        var directory = Path.GetDirectoryName(targetFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempFilePath = targetFilePath + ".tmp";
        File.WriteAllText(tempFilePath, content);

        if (File.Exists(targetFilePath))
        {
            File.Replace(tempFilePath, targetFilePath, destinationBackupFileName: null);
        }
        else
        {
            File.Move(tempFilePath, targetFilePath);
        }
    }
}
