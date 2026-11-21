using MediatR;

namespace CPDLCServer.AdscContracts.Reports;

public record AdscReportExpiredNotification(string Callsign, string StationId) : INotification;
