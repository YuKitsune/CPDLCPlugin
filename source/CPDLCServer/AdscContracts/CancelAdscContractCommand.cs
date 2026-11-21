using MediatR;

namespace CPDLCServer.AdscContracts;

public record CancelAdscContractCommand(
    string StationId,
    string Callsign)
    : IRequest;
