using CPDLCServer.AdscContracts;
using CPDLCServer.AdscContracts.Reports;
using CPDLCServer.Infrastructure;
using CPDLCServer.Settings;
using MediatR;

namespace CPDLCServer.VatsimStatus;

/// <summary>
/// Creates an ADS-C position report using VATSIM status data.
/// <list type="bullet">
/// <item>The VATSIM reports start after the report delay from when the contract was made, so that the aircraft can send its first report through Hoppie.</item>
/// <item>A report from Hoppie stops the VATSIM reports. The VATSIM reports start again when the Hoppie report times out.</item>
/// <item>A rejected contract stops the VATSIM reports.</item>
/// <item>A report is made only when the contract needs one: a demand request, or a periodic report that is due.</item>
/// </list>
/// </summary>
public class ProduceVatsimAdscReportsRequestHandler(
    IAdscContractRepository adscContractRepository,
    IVatsimPilotRepository vatsimPilotRepository,
    IServerSettingsProvider settingsProvider,
    IClock clock,
    IMediator mediator,
    ILogger logger)
    : IRequestHandler<ProduceVatsimAdscReportsRequest>
{
    public async Task Handle(ProduceVatsimAdscReportsRequest request, CancellationToken cancellationToken)
    {
        var settings = await settingsProvider.Load(cancellationToken);
        var contracts = await adscContractRepository.All(cancellationToken);
        var now = clock.UtcNow();

        foreach (var contract in contracts)
        {
            // A failure for one aircraft must not stop the other aircraft
            try
            {
                await ProduceReport(contract, settings, now, cancellationToken);
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

    async Task ProduceReport(AdscContract contract, ServerSettings settings, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!contract.IsActive)
            return;

        var reportDelay = TimeSpan.FromMinutes(settings.VatsimStatusReportDelayMinutes);
        if (!CanUseVatsimData(contract, reportDelay, now) || !IsReportDue(contract, now))
            return;

        var pilot = await vatsimPilotRepository.Find(contract.Callsign, cancellationToken);
        if (pilot is null)
        {
            logger.Debug("{Callsign} is not in the VATSIM data, cannot make ADS-C report", contract.Callsign);
            return;
        }

        if (!pilot.IsAdscEquipped && !settings.VatsimStatusAllowNonEquippedAircraft)
        {
            logger.Debug("{Callsign} is not ADS-C equipped, cannot make ADS-C report", contract.Callsign);
            return;
        }

        logger.Information(
            "Faking ADS-C report from VATSIM data for {Callsign} to {StationId}",
            contract.Callsign,
            contract.StationId);

        var report = new AdscReport(
            pilot.LastUpdated,
            pilot.Latitude,
            pilot.Longitude,
            pilot.Altitude,
            AdscReportSource.VATSIM);

        contract.LogActivity(now);
        contract.ReportReceived(report);
        await mediator.Publish(new AdscContractChangedNotification(contract), cancellationToken);

        await mediator.Publish(
            new AdscReportReceivedNotification(contract.Callsign, contract.StationId, report, now),
            cancellationToken);
    }

    static bool CanUseVatsimData(AdscContract contract, TimeSpan reportDelay, DateTimeOffset now)
    {
        if (now - contract.Created < reportDelay)
            return false;

        // The source is null when there is no report, or when the last report timed out
        return contract.ReportingSource is null or AdscReportSource.VATSIM;
    }

    static bool IsReportDue(AdscContract contract, DateTimeOffset now)
    {
        if (contract.DemandContractState == AdscContractState.Sent)
            return true;

        if (!contract.HasPeriodicContract || contract.ReportingInterval is null)
            return false;

        if (contract.LastReportReceived is null)
            return true;

        var timeSinceLastReport = now - contract.LastReportReceived.Value;
        return timeSinceLastReport >= contract.ReportingInterval.Value;
    }
}
