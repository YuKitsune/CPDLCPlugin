using CPDLCServer.Cpdlc.Dialogues;
namespace CPDLCServer.Cpdlc.Uplinks;

// TODO: Separate formatted and plaintext contents.

public class UplinkMessage : CpdlcMessage
{
    public UplinkMessage(
        Guid dialogueId,
        int messageId,
        int? messageReference,
        string recipient,
        string senderCallsign,
        CpdlcUplinkResponseType responseType,
        AlertType alertType,
        string content,
        DateTimeOffset sent)
        : base(dialogueId, messageId, messageReference, alertType)
    {
        Recipient = recipient;
        SenderCallsign = senderCallsign;
        ResponseType = responseType;
        Content = content;
        Sent = sent;

        // Uplink messages requiring no response are self-closing
        Closed = responseType == CpdlcUplinkResponseType.NoResponse ? sent : null;

        // Uplinks are automatically acknowledged
        Acknowledged = sent;
    }

    public string Recipient { get; }
    public string SenderCallsign { get; }
    public CpdlcUplinkResponseType ResponseType { get; }
    public string Content { get; }
    public DateTimeOffset Sent { get; }
    public bool ClosedManually { get; private set; }

    // public bool CanAction { get; set; }
    // public bool Actioned { get; set; }
    public bool IsPilotLate { get; set; }
    public bool DidTransmissionFail { get; set; }

    public override DateTimeOffset Time => Sent;

    public override void Close(DateTimeOffset time) => Close(time, false);

    public void Close(DateTimeOffset time, bool manual)
    {
        Closed = time;
        ClosedManually = manual;
        IsPilotLate = false;
    }
}
