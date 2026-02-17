using CPDLCServer.Model;
using MediatR;

namespace CPDLCServer.Messages;

public record AircraftConnected(
    string AcarsClientId,
    string StationId,
    string Callsign,
    DataAuthorityState DataAuthorityState)
    : INotification;
