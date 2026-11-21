using MediatR;

namespace CPDLCServer.AdscContracts.Reports;

public record AdscReportReceivedNotification(
    string Callsign,
    string StationId,
    AdscReport Report,
    DateTimeOffset Received)
    : INotification;
