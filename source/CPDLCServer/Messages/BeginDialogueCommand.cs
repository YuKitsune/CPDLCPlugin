using CPDLCServer.Model;
using MediatR;

namespace CPDLCServer.Messages;

public record BeginDialogueCommand(
    string Sender,
    string Recipient,
    CpdlcUplinkResponseType ResponseType,
    string Content)
    : IRequest;
