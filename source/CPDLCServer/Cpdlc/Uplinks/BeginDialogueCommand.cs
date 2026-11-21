using MediatR;

namespace CPDLCServer.Cpdlc.Uplinks;

public record BeginDialogueCommand(
    string Sender,
    string Recipient,
    CpdlcUplinkResponseType ResponseType,
    string Content)
    : IRequest;
