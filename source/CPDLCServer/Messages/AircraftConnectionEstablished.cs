using CPDLCServer.Model;
using MediatR;

namespace CPDLCServer.Messages;

public record AircraftConnectionEstablished(
    string AcarsClientId,
    string StationId,
    string Callsign,
    DataAuthorityState DataAuthorityState)
    : INotification;
