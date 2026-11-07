using CPDLCServer.Cpdlc.Dialogues;
namespace CPDLCServer.Cpdlc.Downlinks;

public class DownlinkMessage : CpdlcMessage
{
    public DownlinkMessage(
        Guid dialogueId,
        int messageId,
        int? messageReference,
        string sender,
        CpdlcDownlinkResponseType responseType,
        AlertType alertType,
        string content,
        DateTimeOffset received)
        : base(dialogueId, messageId, messageReference, alertType)
    {
        Sender = sender;
        ResponseType = responseType;
        Content = content;
        Received = received;

        // Downlink messages requiring no response are self-closing
        Closed = responseType == CpdlcDownlinkResponseType.NoResponse ? received : null;
    }

    public string Sender { get; }
    public CpdlcDownlinkResponseType ResponseType { get; }
    public string Content { get; }
    public DateTimeOffset Received { get; }

    public bool IsControllerLate { get; set; }

    public override DateTimeOffset Time => Received;

    public override void Close(DateTimeOffset time)
    {
        Closed = time;
        IsControllerLate = false;
    }

    public void Acknowledge(DateTimeOffset now)
    {
        Acknowledged = now;
    }
}
