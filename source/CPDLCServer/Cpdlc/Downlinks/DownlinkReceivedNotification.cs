using MediatR;

namespace CPDLCServer.Cpdlc.Downlinks;

public record DownlinkReceivedNotification(
    string AcarsConnectionId,
    string StationId,
    ReceivedDownlink Downlink)
    : INotification;
