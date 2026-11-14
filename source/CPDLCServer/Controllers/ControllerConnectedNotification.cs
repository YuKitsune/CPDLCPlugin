using MediatR;

namespace CPDLCServer.Controllers;

public record ControllerConnectedNotification(Guid UserId, string StationId, string Callsign) : INotification;
