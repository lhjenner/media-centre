// Raised when the Videos root folder cannot be scanned or is invalid.
using System;

namespace MediaPlayerApp.Domain.Exceptions;

public class LibraryScanException : MediaPlayerAppException
{
    public LibraryScanException()
    {
    }

    public LibraryScanException(string message)
        : base(message)
    {
    }

    public LibraryScanException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
