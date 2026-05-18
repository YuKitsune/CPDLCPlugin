using CPDLCServer.Model;
using MediatR;

namespace CPDLCServer.Messages;

public record NewDialogueFromDownlinkNotification(DownlinkMessage Downlink) : INotification;
