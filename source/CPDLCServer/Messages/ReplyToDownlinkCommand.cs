using CPDLCServer.Model;
using MediatR;

namespace CPDLCServer.Messages;

public record ReplyToDownlinkCommand(
    string Sender,
    Guid DialogueId,
    int DownlinkMessageId,
    CpdlcUplinkResponseType ResponseType,
    string Content)
    : IRequest;
