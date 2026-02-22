using MediatR;

namespace CPDLCServer.Messages;

public record AircraftConnectionTerminated(
    string AcarsClientId,
    string StationId,
    string Callsign)
    : INotification;
