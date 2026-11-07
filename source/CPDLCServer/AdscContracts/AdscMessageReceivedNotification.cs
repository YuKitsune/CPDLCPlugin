using MediatR;

namespace CPDLCServer.AdscContracts;

public record AdscMessageReceivedNotification(
    string AcarsConnectionId,
    string StationId,
    ReceivedAdscMessage Message)
    : INotification;
