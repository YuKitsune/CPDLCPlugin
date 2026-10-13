using CPDLCServer.Model;
using MediatR;

namespace CPDLCServer.Messages;

public record AdscReportReceivedNotification(
    string Callsign,
    string StationId,
    AdscReport Report,
    DateTimeOffset Received)
    : INotification;
