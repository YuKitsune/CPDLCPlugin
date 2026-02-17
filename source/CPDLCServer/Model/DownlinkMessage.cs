namespace CPDLCServer.Model;

public class DownlinkMessage(
    int messageId,
    int? messageReference,
    string sender,
    CpdlcDownlinkResponseType responseType,
    AlertType alertType,
    string content,
    DateTimeOffset received)
    : ICpdlcMessage
{
    public int MessageId { get; } = messageId;
    public int? MessageReference { get; } = messageReference;
    public string Sender { get; } = sender;
    public CpdlcDownlinkResponseType ResponseType { get; } = responseType;
    public AlertType AlertType { get; } = alertType;
    public string Content { get; } = content;
    public DateTimeOffset Received { get; } = received;

    public DateTimeOffset? Closed { get; private set; } = responseType == CpdlcDownlinkResponseType.NoResponse ? received : null; // Downlink messages requiring no response are self-closing
    public bool IsClosed => Closed is not null;

    public DateTimeOffset? Acknowledged { get; private set; }
    public bool IsAcknowledged => Acknowledged is not null;

    public bool IsControllerLate { get; set; }

    DateTimeOffset ICpdlcMessage.Time => Received;

    public void Close(DateTimeOffset time)
    {
        Closed = time;
    }

    public void Acknowledge(DateTimeOffset now)
    {
        Acknowledged = now;
    }
}
