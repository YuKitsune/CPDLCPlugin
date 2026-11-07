using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public record GetConnectedAircraftRequest : IRequest<GetConnectedAircraftResult>;

public record GetConnectedAircraftResult(AircraftConnectionDto[] Aircraft);
