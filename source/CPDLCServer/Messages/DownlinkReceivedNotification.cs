using CPDLCServer.Model;
using MediatR;

namespace CPDLCServer.Messages;

public record DownlinkReceivedNotification(
    string AcarsClientId,
    string StationId,
    DownlinkMessage Downlink)
    : INotification;
