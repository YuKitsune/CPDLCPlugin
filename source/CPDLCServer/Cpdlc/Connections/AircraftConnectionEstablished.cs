using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public record AircraftConnectionEstablished(
    string AcarsConnectionId,
    string StationId,
    string Callsign,
    DataAuthorityState DataAuthorityState)
    : INotification;
