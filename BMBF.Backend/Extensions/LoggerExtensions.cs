using Serilog;

namespace BMBF.Backend.Extensions;

public static class LoggerExtensions
{
    /// <summary>
    /// Sets the "MessageLogContext" property of the passed logger to <paramref name="context"/>
    /// </summary>
    /// <param name="logger">Logger to attach context</param>
    /// <param name="context">The context to attach</param>
    /// <returns>A logger with <paramref name="context"/></returns>
    public static ILogger ForContext(this ILogger logger, LogType context)
    {
        return logger.ForContext("MessageLogContext", context);
    }
}
