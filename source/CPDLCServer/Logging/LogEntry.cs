namespace CPDLCServer.Logging;

// A log event, as the Logs page shows it.
// The sequence number increases by one for each entry, so that a client can ignore the entries that it already has.
public record LogEntry(
    long Sequence,
    string Time,
    string Level,
    string LevelCode,
    string Message,
    string? Exception);
