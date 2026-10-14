using MediatR;

namespace CPDLCServer.Messages;

public record RequestSelectedAdscContractCommand(
    string StationId,
    string Callsign,
    TimeSpan ReportingInterval)
    : IRequest;
