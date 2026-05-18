using CPDLCServer.Model;
using MediatR;

namespace CPDLCServer.Messages;

public record AircraftRepliedToUplinkNotification(DownlinkMessage Downlink) : INotification;
