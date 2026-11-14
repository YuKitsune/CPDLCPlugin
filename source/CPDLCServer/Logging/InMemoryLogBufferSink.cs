using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace CPDLCServer.Logging;

/// <summary>
/// A log sink that stores log events in-memory.
/// </summary>
public class InMemoryLogBufferSink(LogBuffer logBuffer) : ILogEventSink
{
    // The same text as the console output, without the timestamp, the level and the exception
    readonly MessageTemplateTextFormatter _messageFormatter = new("{Message:lj}");

    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        _messageFormatter.Format(logEvent, writer);

        logBuffer.Add(new LogEntry(
            Sequence: 0, // The LogBuffer sets the sequence number
            Time: logEvent.Timestamp.ToString("HH:mm:ss"),
            Level: logEvent.Level.ToString(),
            LevelCode: LevelCode(logEvent.Level),
            Message: writer.ToString(),
            Exception: logEvent.Exception?.ToString()));
    }

    // The same codes as {Level:u3} in the console output
    static string LevelCode(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => "VRB",
            LogEventLevel.Debug => "DBG",
            LogEventLevel.Information => "INF",
            LogEventLevel.Warning => "WRN",
            LogEventLevel.Error => "ERR",
            LogEventLevel.Fatal => "FTL",
            _ => level.ToString().ToUpperInvariant()
        };
    }
}
