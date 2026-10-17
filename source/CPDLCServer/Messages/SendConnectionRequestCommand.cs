using MediatR;

namespace CPDLCServer.Messages;

public record SendConnectionRequestCommand(
    string Callsign,
    string StationId)
    : IRequest;
