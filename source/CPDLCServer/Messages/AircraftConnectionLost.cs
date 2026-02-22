using MediatR;

namespace CPDLCServer.Messages;

public record AircraftConnectionLost(
    string AcarsClientId,
    string Callsign)
    : INotification;
