using MediatR;

namespace CPDLCServer.Cpdlc.Uplinks;

public record AcknowledgeUplinkCommand(Guid DialogueId, int UplinkMessageId) : IRequest;