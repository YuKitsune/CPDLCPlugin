using CPDLC.Messages;
using CPDLCServer.Cpdlc.Dialogues;
namespace CPDLCServer.Cpdlc.Downlinks;

public class DownlinkMessage : CpdlcMessage
{
    // The response type comes from the ACARS packet, and not from the elements.
    // If the ACARS connection cannot read the text, the element is free text, which needs no response.
    public DownlinkMessage(
        Guid dialogueId,
        int messageId,
        int? messageReference,
        string sender,
        CpdlcDownlinkResponseType responseType,
        IReadOnlyList<DownlinkMessageElement> elements,
        DateTimeOffset received)
        : base(dialogueId, messageId, messageReference, AlertTypeMapping.From(MessageAttributes.AlertAttribute(elements)))
    {
        Sender = sender;
        ResponseType = responseType;
        Elements = elements;
        Received = received;

        // Downlink messages requiring no response are self-closing
        Closed = responseType == CpdlcDownlinkResponseType.NoResponse ? received : null;
    }

    public string Sender { get; }
    public CpdlcDownlinkResponseType ResponseType { get; }
    public IReadOnlyList<DownlinkMessageElement> Elements { get; }
    public DateTimeOffset Received { get; }

    public bool IsControllerLate { get; set; }

    public override DateTimeOffset Time => Received;

    public bool HasElement(int elementId) => Elements.Any(e => e.Id == elementId);

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
