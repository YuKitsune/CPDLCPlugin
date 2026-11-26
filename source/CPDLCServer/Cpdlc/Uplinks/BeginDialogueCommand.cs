using CPDLC.Messages;
using MediatR;

namespace CPDLCServer.Cpdlc.Uplinks;

public record BeginDialogueCommand(
    string Sender,
    string Recipient,
    IReadOnlyList<UplinkMessageElement> Elements,
    bool CanAction)
    : IRequest;
