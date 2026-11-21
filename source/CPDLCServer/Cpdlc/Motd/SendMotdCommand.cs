using MediatR;

namespace CPDLCServer.Cpdlc.Motd;

public record SendMotdCommand(string AcarsConnectionId, string Callsign) : IRequest;
