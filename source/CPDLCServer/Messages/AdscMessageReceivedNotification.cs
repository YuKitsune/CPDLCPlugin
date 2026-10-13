using CPDLCServer.Model;
using MediatR;

namespace CPDLCServer.Messages;

public record AdscMessageReceivedNotification(
    string AcarsClientId,
    string StationId,
    ReceivedAdscMessage Message)
    : INotification;
