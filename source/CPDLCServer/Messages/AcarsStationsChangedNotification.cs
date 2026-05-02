using MediatR;

namespace CPDLCServer.Messages;

public record AcarsStationsChangedNotification(string[] Callsigns) : INotification;
