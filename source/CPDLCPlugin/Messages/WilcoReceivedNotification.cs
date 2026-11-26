using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCPlugin.Messages;

// Published when the pilot sends WILCO to an uplink.
// A handler can update the FDR from the uplink, and then send MarkUplinkActionedRequest.
public record WilcoReceivedNotification(DialogueDto Dialogue, UplinkMessageDto Uplink) : INotification;
