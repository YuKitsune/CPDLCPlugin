using MediatR;

namespace CPDLCServer.Messages;

public record RequestDefaultAdscContractCommand(
    string StationId,
    string Callsign)
    : IRequest;
