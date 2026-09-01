// Defines a simple logging abstraction used across layers.
using System;

namespace MediaPlayerApp.Common.Logging;

public interface ILogger
{
    void Info(string message);

    void Warning(string message);

    void Error(string message, Exception? exception = null);
}
