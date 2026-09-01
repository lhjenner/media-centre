// Contract for generating episode thumbnail images.
namespace MediaPlayerApp.Domain.Abstractions;

public interface IThumbnailGenerator
{
    /// <summary>
    /// Generates a thumbnail snapshot from the video at <paramref name="videoFilePath"/>, seeking to
    /// <paramref name="timestampSeconds"/>, and writes the resulting image to <paramref name="outputFilePath"/>.
    /// </summary>
    void GenerateThumbnail(string videoFilePath, string outputFilePath, int timestampSeconds);
}
