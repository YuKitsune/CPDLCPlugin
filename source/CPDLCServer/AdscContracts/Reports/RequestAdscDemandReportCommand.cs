using MediatR;

namespace CPDLCServer.AdscContracts.Reports;

public record RequestAdscDemandReportCommand(
    string StationId,
    string Callsign)
    : IRequest;
