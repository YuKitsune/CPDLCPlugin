using MediatR;

namespace CPDLCServer.Cpdlc.Uplinks;

public record MarkUplinkTransmissionFailedCommand(Guid DialogueId, int UplinkMessageId) : IRequest;
