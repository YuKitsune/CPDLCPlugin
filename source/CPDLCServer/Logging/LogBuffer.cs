using System.Threading.Channels;

namespace CPDLCServer.Logging;

// Keeps the most recent log entries in memory, and sends each new entry to the subscribers
public class LogBuffer(int capacity = 1000)
{
    readonly object _lock = new();
    readonly Queue<LogEntry> _entries = new();
    readonly List<Channel<LogEntry>> _subscribers = [];
    long _nextSequence = 1;

    public void Add(LogEntry entry)
    {
        lock (_lock)
        {
            var sequencedEntry = entry with { Sequence = _nextSequence++ };

            _entries.Enqueue(sequencedEntry);
            if (_entries.Count > capacity)
                _entries.Dequeue();

            foreach (var subscriber in _subscribers)
                subscriber.Writer.TryWrite(sequencedEntry);
        }
    }

    // The subscription gets the recent entries, then each new entry.
    // A slow subscriber loses its oldest entries, so that it cannot use too much memory.
    public LogSubscription Subscribe()
    {
        lock (_lock)
        {
            var channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true
            });

            _subscribers.Add(channel);

            return new LogSubscription(_entries.ToArray(), channel.Reader, () => Unsubscribe(channel));
        }
    }

    void Unsubscribe(Channel<LogEntry> channel)
    {
        lock (_lock)
        {
            _subscribers.Remove(channel);
        }

        channel.Writer.TryComplete();
    }
}

public sealed class LogSubscription(LogEntry[] recentEntries, ChannelReader<LogEntry> newEntries, Action unsubscribe)
    : IDisposable
{
    public LogEntry[] RecentEntries { get; } = recentEntries;
    public ChannelReader<LogEntry> NewEntries { get; } = newEntries;

    public void Dispose() => unsubscribe();
}
