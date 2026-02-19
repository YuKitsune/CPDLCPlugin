using MediatR;

namespace CPDLCServer.Messages;

public record CheckAircraftConnectionsRequest(string AcarsClientId) : IRequest;
