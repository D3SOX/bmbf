using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace BMBF.QMod;

/// <summary>
/// Log sink that adds indentation to the log events passed to it.
/// </summary>
internal class IndentationSink : ILogEventSink
{
    private readonly ILogger _writeTo;
    private readonly string _indentation;
    
    /// <summary>
    /// Creates a new <see cref="IndentationSink"/>
    /// </summary>
    /// <param name="writeTo">The underlying logger to write to</param>
    /// <param name="indentation">The number of spaces to prepend to each message</param>
    internal IndentationSink(ILogger writeTo, int indentation)
    {
        _writeTo = writeTo;
        _indentation = new string(' ', indentation);
    }

    public void Emit(LogEvent logEvent)
    {
        var msg = logEvent.RenderMessage();
        _writeTo.Write(logEvent.Level, _indentation + msg);
    }
}
