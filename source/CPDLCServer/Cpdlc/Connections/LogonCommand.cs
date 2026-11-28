using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public record LogonCommand(string Callsign, string AcarsConnectionId) : IRequest;
