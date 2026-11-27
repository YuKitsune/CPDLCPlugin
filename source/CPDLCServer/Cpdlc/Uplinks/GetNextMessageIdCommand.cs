using MediatR;

namespace CPDLCServer.Cpdlc.Uplinks;

public record GetNextMessageIdCommand(string AcarsConnectionId, string Callsign) : IRequest<int>;
