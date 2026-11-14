namespace CPDLCServer.Cpdlc.Uplinks;

// The last CPDLC message ID that the server sent to an aircraft on an ACARS connection
public class MessageIdSequence(string acarsConnectionId, string callsign)
{
    public string AcarsConnectionId { get; } = acarsConnectionId;
    public string Callsign { get; } = callsign;
    public int LastMessageId { get; private set; }

    public int Next()
    {
        LastMessageId++;
        return LastMessageId;
    }
}
