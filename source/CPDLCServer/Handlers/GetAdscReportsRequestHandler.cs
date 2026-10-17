using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class GetAdscReportsRequestHandler(IAdscReportRepository adscReportRepository)
    : IRequestHandler<GetAdscReportsRequest, GetAdscReportsResult>
{
    public async Task<GetAdscReportsResult> Handle(
        GetAdscReportsRequest request,
        CancellationToken cancellationToken)
    {
        var reports = await adscReportRepository.All(cancellationToken);
        var reportDtos = reports
            .Select(r => AdscContractConverter.ToDto(r.Callsign, r.StationId, r.Report, r.Received))
            .ToArray();

        return new GetAdscReportsResult(reportDtos);
    }
}
