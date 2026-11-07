using MediatR;

namespace CPDLCServer.AcarsConnections.Stations;

public record AcarsStationsChangedNotification(string[] Callsigns) : INotification;
