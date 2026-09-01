// Raised when LibVLCSharp fails to load or play a media file.
namespace MediaPlayerApp.Domain.Exceptions;

public class MediaPlaybackException : MediaPlayerAppException
{
    public MediaPlaybackException()
    {
    }

    public MediaPlaybackException(string message)
        : base(message)
    {
    }

    public MediaPlaybackException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
