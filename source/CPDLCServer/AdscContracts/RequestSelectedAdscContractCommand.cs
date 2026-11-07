using MediatR;

namespace CPDLCServer.AdscContracts;

public record RequestSelectedAdscContractCommand(
    string StationId,
    string Callsign,
    TimeSpan ReportingInterval)
    : IRequest;
