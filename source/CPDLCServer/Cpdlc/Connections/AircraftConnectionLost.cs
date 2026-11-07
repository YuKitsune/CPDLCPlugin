using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public record AircraftConnectionLost(
    string AcarsConnectionId,
    string Callsign)
    : INotification;
