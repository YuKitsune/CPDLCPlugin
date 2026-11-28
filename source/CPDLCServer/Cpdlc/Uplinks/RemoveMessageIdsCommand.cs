using MediatR;

namespace CPDLCServer.Cpdlc.Uplinks;

public record RemoveMessageIdsCommand(string AcarsConnectionId, string Callsign) : IRequest;
