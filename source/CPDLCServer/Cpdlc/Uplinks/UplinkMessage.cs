using CPDLC.Messages;
using CPDLCServer.Cpdlc.Dialogues;
namespace CPDLCServer.Cpdlc.Uplinks;

public class UplinkMessage : CpdlcMessage
{
    public UplinkMessage(
        Guid dialogueId,
        int messageId,
        int? messageReference,
        string recipient,
        string senderCallsign,
        IReadOnlyList<UplinkMessageElement> elements,
        bool canAction,
        DateTimeOffset sent)
        : base(dialogueId, messageId, messageReference, AlertTypeMapping.From(MessageAttributes.AlertAttribute(elements)))
    {
        Recipient = recipient;
        SenderCallsign = senderCallsign;
        ResponseType = CpdlcUplinkResponseTypeMapping.From(MessageAttributes.ResponseAttribute(elements));
        Elements = elements;
        CanAction = canAction;
        Sent = sent;

        // Uplink messages requiring no response are self-closing
        Closed = ResponseType == CpdlcUplinkResponseType.NoResponse ? sent : null;

        // Uplinks are automatically acknowledged
        Acknowledged = sent;
    }

    public string Recipient { get; }
    public string SenderCallsign { get; }
    public CpdlcUplinkResponseType ResponseType { get; }
    public IReadOnlyList<UplinkMessageElement> Elements { get; }
    public DateTimeOffset Sent { get; }
    public bool ClosedManually { get; private set; }

    // CanAction is true when the plugin can update the FDR from the message.
    // Actioned is true when the plugin updated the FDR from the message.
    public bool CanAction { get; }
    public bool Actioned { get; private set; }

    public bool IsPilotLate { get; set; }
    public bool DidTransmissionFail { get; set; }

    public override DateTimeOffset Time => Sent;

    public bool HasElement(int elementId) => Elements.Any(e => e.Id == elementId);

    public override void Close(DateTimeOffset time) => Close(time, false);

    public void Close(DateTimeOffset time, bool manual)
    {
        Closed = time;
        ClosedManually = manual;
        IsPilotLate = false;
    }

    public void MarkActioned()
    {
        Actioned = true;
    }
}
