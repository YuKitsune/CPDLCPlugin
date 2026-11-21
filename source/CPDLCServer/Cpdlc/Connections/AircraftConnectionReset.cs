using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

// A new logon replaced the connection of an aircraft. The pilot can be a different pilot with the same callsign.
public record AircraftConnectionReset(
    string AcarsConnectionId,
    string StationId,
    string Callsign)
    : INotification;
