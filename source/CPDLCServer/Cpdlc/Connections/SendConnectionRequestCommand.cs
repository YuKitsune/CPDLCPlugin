using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public record SendConnectionRequestCommand(
    string Callsign,
    string StationId)
    : IRequest;
