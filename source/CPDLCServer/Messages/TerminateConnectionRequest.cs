using MediatR;

namespace CPDLCServer.Messages;

public record TerminateConnectionRequest(
    string Callsign,
    string AcarsClientId) : IRequest;
