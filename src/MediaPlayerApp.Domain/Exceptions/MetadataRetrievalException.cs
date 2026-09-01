// Raised when an online/local metadata lookup fails unexpectedly.
using System;

namespace MediaPlayerApp.Domain.Exceptions;

public class MetadataRetrievalException : MediaPlayerAppException
{
    public MetadataRetrievalException()
    {
    }

    public MetadataRetrievalException(string message)
        : base(message)
    {
    }

    public MetadataRetrievalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
