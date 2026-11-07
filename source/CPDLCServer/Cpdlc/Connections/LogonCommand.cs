using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Downlinks;
using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public record LogonCommand(
    int DownlinkId,
    int? DownlinkMessageReference,
    string Callsign,
    CpdlcDownlinkResponseType DownlinkResponseType,
    AlertType DownlinkAlertType,
    string DownlinkContent,
    DateTimeOffset DownlinkReceived,
    string AcarsConnectionId) : IRequest;
