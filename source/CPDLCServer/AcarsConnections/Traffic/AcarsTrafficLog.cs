namespace CPDLCServer.AcarsConnections.Traffic;

/// <summary>
/// Keeps the most recent requests to the ACARS network and their responses for each ACARS connection, so that an
/// administrator can see the raw data. The entries are kept only in memory, so a restart clears them.
/// </summary>
public class AcarsTrafficLog(int capacity = 500)
{
    public const string RedactedValue = "***";

    // The names of the request fields that contain a secret
    static readonly string[] SecretFields = ["logon"];

    public int Capacity { get; } = capacity;

    readonly Lock _lock = new();
    readonly Dictionary<string, Queue<AcarsTrafficEntry>> _entries = new();

    public void Add(string acarsConnectionId, AcarsTrafficEntry entry)
    {
        lock (_lock)
        {
            if (!_entries.TryGetValue(acarsConnectionId, out var entries))
            {
                entries = new Queue<AcarsTrafficEntry>();
                _entries[acarsConnectionId] = entries;
            }

            entries.Enqueue(entry);
            if (entries.Count > Capacity)
                entries.Dequeue();
        }
    }

    // Newest entries first
    public AcarsTrafficEntry[] Get(string acarsConnectionId)
    {
        lock (_lock)
        {
            return _entries.TryGetValue(acarsConnectionId, out var entries)
                ? entries.Reverse().ToArray()
                : [];
        }
    }

    // The request fields as text, one field on each line. The secrets are replaced.
    public static string FormatRequest(IReadOnlyDictionary<string, string> fields)
    {
        return string.Join(
            '\n',
            fields.Select(field => $"{field.Key}={(SecretFields.Contains(field.Key) ? RedactedValue : field.Value)}"));
    }
}
