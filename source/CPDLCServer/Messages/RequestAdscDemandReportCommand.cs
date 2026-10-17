using MediatR;

namespace CPDLCServer.Messages;

public record RequestAdscDemandReportCommand(
    string StationId,
    string Callsign)
    : IRequest;
