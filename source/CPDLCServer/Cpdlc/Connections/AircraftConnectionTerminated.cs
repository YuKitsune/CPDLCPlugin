using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public record AircraftConnectionTerminated(
    string AcarsConnectionId,
    string StationId,
    string Callsign)
    : INotification;
