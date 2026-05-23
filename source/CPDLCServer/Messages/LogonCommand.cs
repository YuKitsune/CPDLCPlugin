using CPDLCServer.Model;
using MediatR;

namespace CPDLCServer.Messages;

public record LogonCommand(
    int DownlinkId,
    int? DownlinkMessageReference,
    string Callsign,
    CpdlcDownlinkResponseType DownlinkResponseType,
    AlertType DownlinkAlertType,
    string DownlinkContent,
    DateTimeOffset DownlinkReceived,
    string AcarsClientId) : IRequest;
