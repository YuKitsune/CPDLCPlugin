using MediatR;

namespace CPDLCServer.Messages;

public record RequestPeriodicAdscContractCommand(
    string StationId,
    string Callsign,
    TimeSpan ReportingInterval)
    : IRequest;
