using CPDLC.Messages;
using MediatR;

namespace CPDLCServer.Cpdlc.Uplinks;

public record ReplyToDownlinkCommand(
    string Sender,
    Guid DialogueId,
    int DownlinkMessageId,
    IReadOnlyList<UplinkMessageElement> Elements,
    bool CanAction)
    : IRequest;
