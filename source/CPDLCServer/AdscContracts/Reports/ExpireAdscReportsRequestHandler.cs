using CPDLCServer.Infrastructure;
using MediatR;

namespace CPDLCServer.AdscContracts.Reports;

/// <summary>
/// Removes the ADS-C reports that are too old to show the position of the aircraft.
/// <list type="bullet">
/// <item>A report expires when the time since the last report of its contract is more than the report expiry of the contract.</item>
/// <item>A report without a contract expires immediately, because the aircraft does not send more reports.</item>
/// </list>
/// </summary>
public class ExpireAdscReportsRequestHandler(
    IAdscReportRepository adscReportRepository,
    IAdscContractRepository adscContractRepository,
    IClock clock,
    IMediator mediator,
    ILogger logger)
    : IRequestHandler<ExpireAdscReportsRequest>
{
    public async Task Handle(ExpireAdscReportsRequest request, CancellationToken cancellationToken)
    {
        var reports = await adscReportRepository.All(cancellationToken);
        var now = clock.UtcNow();

        foreach (var report in reports)
        {
            var contract = await adscContractRepository.Find(
                new AdscContractKey(report.Callsign, report.StationId),
                cancellationToken);

            if (contract?.LastReportReceived is not null && now - contract.LastReportReceived.Value <= contract.ReportExpiry)
                continue;

            logger.Information(
                "ADS-C report from {Callsign} to {StationId} at {Timestamp} has expired",
                report.Callsign,
                report.StationId,
                report.Report.Timestamp);

            contract?.ReportTimedOut();
            await adscReportRepository.Remove(report, cancellationToken);

            await mediator.Publish(
                new AdscReportExpiredNotification(report.Callsign, report.StationId),
                cancellationToken);
        }
    }
}
