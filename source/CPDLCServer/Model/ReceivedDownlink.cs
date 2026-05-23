namespace CPDLCServer.Model;

// Raw downlink data as parsed from an ACARS packet, before being assigned to a dialogue.
public record ReceivedDownlink(
    int MessageId,
    int? MessageReference,
    string Sender,
    CpdlcDownlinkResponseType ResponseType,
    AlertType AlertType,
    string Content,
    DateTimeOffset Received);
