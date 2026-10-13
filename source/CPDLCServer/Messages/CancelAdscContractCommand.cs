using MediatR;

namespace CPDLCServer.Messages;

public record CancelAdscContractCommand(
    string StationId,
    string Callsign)
    : IRequest;
