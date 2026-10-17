using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Services;
using MediatR;

namespace CPDLCServer.Handlers;

/// <summary>
/// Makes ADS-C reports from the VATSIM status data, for aircraft that do not send ADS-C reports through Hoppie.
/// <list type="bullet">
/// <item>A report from Hoppie stops the VATSIM reports. The VATSIM reports start again when the Hoppie reports are more than two reporting intervals late.</item>
/// <item>A rejected contract stops the VATSIM reports.</item>
/// <item>A report is made only when the contract needs one: a demand request, or a periodic report that is due.</item>
/// </list>
/// </summary>
public class ProduceVatsimAdscReportsRequestHandler(
    IAdscContractRepository adscContractRepository,
    IAdscReportRepository adscReportRepository,
    IVatsimPilotRepository vatsimPilotRepository,
    VatsimStatusConfiguration configuration,
    IClock clock,
    IMediator mediator,
    ILogger logger)
    : IRequestHandler<ProduceVatsimAdscReportsRequest>
{
    public async Task Handle(ProduceVatsimAdscReportsRequest request, CancellationToken cancellationToken)
    {
        var contracts = await adscContractRepository.All(cancellationToken);
        var now = clock.UtcNow();

        foreach (var contract in contracts)
        {
            // A failure for one aircraft must not stop the other aircraft
            try
            {
                await ProduceReport(contract, now, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.Error(
                    ex,
                    "Failed to make ADS-C report from VATSIM data for {Callsign} to {StationId}",
                    contract.Callsign,
                    contract.StationId);
            }
        }
    }

    async Task ProduceReport(AdscContract contract, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!contract.IsActive)
            return;

        var latestReport = await adscReportRepository.Find(contract.Callsign, cancellationToken);
        if (!CanUseVatsimData(contract, latestReport, now) || !IsReportDue(contract, latestReport, now))
            return;

        var pilot = await vatsimPilotRepository.Find(contract.Callsign, cancellationToken);
        if (pilot is null)
        {
            logger.Debug("{Callsign} is not in the VATSIM data, cannot make ADS-C report", contract.Callsign);
            return;
        }

        if (!pilot.IsAdscEquipped && !configuration.AllowNonEquippedAircraft)
        {
            logger.Debug("{Callsign} is not ADS-C equipped, cannot make ADS-C report", contract.Callsign);
            return;
        }

        logger.Information(
            "Made ADS-C report from VATSIM data for {Callsign} to {StationId}",
            contract.Callsign,
            contract.StationId);

        if (contract.ReportReceived())
        {
            logger.Information(
                "{Callsign} accepted the ADS-C contract with {StationId}",
                contract.Callsign,
                contract.StationId);

            await mediator.Publish(new AdscContractChangedNotification(contract), cancellationToken);
        }

        var report = new AdscReport(
            pilot.LastUpdated,
            pilot.Latitude,
            pilot.Longitude,
            pilot.Altitude,
            AdscReportSource.VATSIM);

        await mediator.Publish(
            new AdscReportReceivedNotification(contract.Callsign, contract.StationId, report, now),
            cancellationToken);
    }

    // VATSIM data is used only when there is no report from Hoppie, or when the Hoppie reports are late
    static bool CanUseVatsimData(AdscContract contract, AircraftAdscReport? latestReport, DateTimeOffset now)
    {
        if (latestReport is null || latestReport.Report.Source == AdscReportSource.VATSIM)
            return true;

        // Only a periodic contract makes the reports late
        if (!contract.HasPeriodicContract || contract.ReportingInterval is null)
            return false;

        return now - latestReport.Received > contract.ReportingInterval.Value * 2;
    }

    static bool IsReportDue(AdscContract contract, AircraftAdscReport? latestReport, DateTimeOffset now)
    {
        if (contract.DemandContractState == AdscContractState.Sent)
            return true;

        if (!contract.HasPeriodicContract || contract.ReportingInterval is null)
            return false;

        return latestReport is null || now - latestReport.Received >= contract.ReportingInterval.Value;
    }
}
