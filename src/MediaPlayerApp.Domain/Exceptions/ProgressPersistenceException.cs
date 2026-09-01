// Raised when the progress JSON file cannot be read or written.
using System;

namespace MediaPlayerApp.Domain.Exceptions;

public class ProgressPersistenceException : MediaPlayerAppException
{
    public ProgressPersistenceException()
    {
    }

    public ProgressPersistenceException(string message)
        : base(message)
    {
    }

    public ProgressPersistenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
