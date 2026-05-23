namespace CPDLCServer.Model;

// TODO: Separate formatted and plaintext contents.

public class UplinkMessage(
    Guid dialogueId,
    int messageId,
    int? messageReference,
    string recipient,
    string senderCallsign,
    CpdlcUplinkResponseType responseType,
    AlertType alertType,
    string content,
    DateTimeOffset sent)
    : ICpdlcMessage
{
    public Guid DialogueId { get; } = dialogueId;
    public int MessageId { get; } = messageId;
    public int? MessageReference { get; } = messageReference;
    public string Recipient { get; } = recipient;
    public string SenderCallsign { get; } = senderCallsign;
    public CpdlcUplinkResponseType ResponseType { get; } = responseType;
    public AlertType AlertType { get; } = alertType;
    public string Content { get; } = content;
    public DateTimeOffset Sent { get; } = sent;
    public DateTimeOffset? Closed { get; private set; } = responseType == CpdlcUplinkResponseType.NoResponse ? sent : null; // Uplink messages requiring no response are self-closing
    public bool IsClosed => Closed is not null;
    public bool ClosedManually { get; private set; }

    // Uplinks are automatically acknowledged
    DateTimeOffset? ICpdlcMessage.Acknowledged => Sent;
    public bool IsAcknowledged => true;

    // public bool CanAction { get; set; }
    // public bool Actioned { get; set; }
    public bool IsPilotLate { get; set; }
    public bool IsTransmissionFailed { get; set; }

    DateTimeOffset ICpdlcMessage.Time => Sent;
    
    public void Close(DateTimeOffset time, bool manual = false)
    {
        Closed = time;
        ClosedManually = manual;
        IsPilotLate = false;
    }

    void ICpdlcMessage.Close(DateTimeOffset time) => Close(time, false);
}