using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public record TerminateConnectionRequest(
    string Callsign,
    string AcarsConnectionId) : IRequest;
