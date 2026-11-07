using CPDLCServer.Logging;
using Serilog;
using Serilog.Events;

namespace CPDLCServer.Tests.Logging;

public class LogBufferTests
{
    [Fact]
    public void Add_KeepsOnlyTheMostRecentEntries()
    {
        // Arrange
        var logBuffer = new LogBuffer(capacity: 2);

        // Act
        logBuffer.Add(CreateEntry("first"));
        logBuffer.Add(CreateEntry("second"));
        logBuffer.Add(CreateEntry("third"));

        // Assert
        using var subscription = logBuffer.Subscribe();
        Assert.Equal(["second", "third"], subscription.RecentEntries.Select(e => e.Message));
    }

    [Fact]
    public void Add_SetsIncreasingSequenceNumbers()
    {
        // Arrange
        var logBuffer = new LogBuffer();

        // Act
        logBuffer.Add(CreateEntry("first"));
        logBuffer.Add(CreateEntry("second"));

        // Assert
        using var subscription = logBuffer.Subscribe();
        Assert.Equal([1L, 2L], subscription.RecentEntries.Select(e => e.Sequence));
    }

    [Fact]
    public void Subscribe_ReceivesNewEntries()
    {
        // Arrange
        var logBuffer = new LogBuffer();
        logBuffer.Add(CreateEntry("before"));
        using var subscription = logBuffer.Subscribe();

        // Act
        logBuffer.Add(CreateEntry("after"));

        // Assert
        Assert.Equal(["before"], subscription.RecentEntries.Select(e => e.Message));
        Assert.True(subscription.NewEntries.TryRead(out var entry));
        Assert.Equal("after", entry.Message);
        Assert.False(subscription.NewEntries.TryRead(out _));
    }

    [Fact]
    public void Dispose_StopsTheSubscription()
    {
        // Arrange
        var logBuffer = new LogBuffer();
        var subscription = logBuffer.Subscribe();

        // Act
        subscription.Dispose();
        logBuffer.Add(CreateEntry("after"));

        // Assert
        Assert.False(subscription.NewEntries.TryRead(out _));
        Assert.True(subscription.NewEntries.Completion.IsCompleted);
    }

    [Fact]
    public void Sink_WritesTheSameTextAsTheConsole()
    {
        // Arrange
        var logBuffer = new LogBuffer();
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(new LogBufferSink(logBuffer))
            .CreateLogger();

        // Act
        logger.ForContext("Station", "YBBB").Warning("Aircraft {Callsign} not seen", "UAL123");
        logger.Error(new InvalidOperationException("Send failed"), "Error sending message");

        // Assert
        using var subscription = logBuffer.Subscribe();
        var warning = subscription.RecentEntries[0];
        Assert.Equal(nameof(LogEventLevel.Warning), warning.Level);
        Assert.Equal("WRN", warning.LevelCode);
        Assert.Equal("Aircraft UAL123 not seen", warning.Message);
        Assert.Null(warning.Exception);

        var error = subscription.RecentEntries[1];
        Assert.Equal("ERR", error.LevelCode);
        Assert.Contains("Send failed", error.Exception);
    }

    static LogEntry CreateEntry(string message)
    {
        return new LogEntry(0, "12:00:00", "Information", "INF", message, null);
    }
}
