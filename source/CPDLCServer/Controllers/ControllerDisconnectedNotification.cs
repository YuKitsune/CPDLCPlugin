using MediatR;

namespace CPDLCServer.Controllers;

public record ControllerDisconnectedNotification(Guid UserId, string Callsign) : INotification;
