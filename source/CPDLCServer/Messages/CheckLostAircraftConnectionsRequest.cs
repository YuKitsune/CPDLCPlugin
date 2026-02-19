using MediatR;

namespace CPDLCServer.Messages;

public record CheckLostAircraftConnectionsRequest(TimeSpan ConnectionTimeout) : IRequest;
