using MediatR;

namespace CPDLCServer.AdscContracts;

public record RequestDefaultAdscContractCommand(
    string StationId,
    string Callsign)
    : IRequest;
