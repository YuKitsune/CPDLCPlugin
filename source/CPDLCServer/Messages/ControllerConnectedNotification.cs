using MediatR;

namespace CPDLCServer.Messages;

public record ControllerConnectedNotification(Guid UserId, string StationId, string Callsign) : INotification;
