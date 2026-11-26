using MediatR;

namespace CPDLCServer.Cpdlc.Uplinks;

public record MarkUplinkActionedCommand(Guid DialogueId, int UplinkMessageId) : IRequest;
