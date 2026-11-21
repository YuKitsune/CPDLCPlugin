using MediatR;

namespace CPDLCServer.Cpdlc.Downlinks;

public record AcknowledgeDownlinkCommand(Guid DialogueId, int DownlinkMessageId) : IRequest;