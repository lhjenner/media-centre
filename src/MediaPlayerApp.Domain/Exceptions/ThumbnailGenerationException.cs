// Raised when a thumbnail snapshot cannot be produced.
using System;

namespace MediaPlayerApp.Domain.Exceptions;

public class ThumbnailGenerationException : MediaPlayerAppException
{
    public ThumbnailGenerationException()
    {
    }

    public ThumbnailGenerationException(string message)
        : base(message)
    {
    }

    public ThumbnailGenerationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
