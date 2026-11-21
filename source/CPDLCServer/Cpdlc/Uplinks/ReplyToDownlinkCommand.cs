using MediatR;

namespace CPDLCServer.Cpdlc.Uplinks;

public record ReplyToDownlinkCommand(
    string Sender,
    Guid DialogueId,
    int DownlinkMessageId,
    CpdlcUplinkResponseType ResponseType,
    string Content)
    : IRequest;
