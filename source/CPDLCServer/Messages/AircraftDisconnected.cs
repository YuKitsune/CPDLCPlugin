using MediatR;

namespace CPDLCServer.Messages;

public record AircraftDisconnected(
    string AcarsClientId,
    string StationId,
    string Callsign)
    : INotification;