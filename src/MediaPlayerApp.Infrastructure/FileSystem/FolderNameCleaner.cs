// Cleans raw folder/file names into display-friendly titles.
using System.Text.RegularExpressions;

namespace MediaPlayerApp.Infrastructure.FileSystem;

public static class FolderNameCleaner
{
    private static readonly Regex SeparatorPattern = new(@"[\.\_]+", RegexOptions.Compiled);
    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Converts a raw folder or file name into a display-friendly title by replacing
    /// separator characters with spaces and trimming redundant whitespace.
    /// </summary>
    public static string Clean(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return rawName;
        }

        var cleaned = SeparatorPattern.Replace(rawName, " ");
        cleaned = WhitespacePattern.Replace(cleaned, " ");
        return cleaned.Trim();
    }
}
