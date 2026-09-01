// Common base type for all custom application exceptions.
namespace MediaPlayerApp.Domain.Exceptions;

public class MediaPlayerAppException : Exception
{
    public MediaPlayerAppException()
    {
    }

    public MediaPlayerAppException(string message)
        : base(message)
    {
    }

    public MediaPlayerAppException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
