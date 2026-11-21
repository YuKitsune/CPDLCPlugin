using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

// The default reporting interval of the ATSU requests the default contract. Any other interval requests a selected contract.
public record SelectAdscReportingIntervalRequest(string Callsign, TimeSpan ReportingInterval) : IRequest;

public class SelectAdscReportingIntervalRequestHandler(
    Plugin plugin,
    ILogger logger)
    : IRequestHandler<SelectAdscReportingIntervalRequest>
{
    public async Task Handle(SelectAdscReportingIntervalRequest request, CancellationToken cancellationToken)
    {
        logger.Verbose(
            "Selecting ADS-C reporting interval {ReportingInterval} for {Callsign}",
            request.ReportingInterval,
            request.Callsign);
        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning(
                "Cannot select ADS-C reporting interval {ReportingInterval} for {Callsign}: not connected to server",
                request.ReportingInterval,
                request.Callsign);
            return;
        }

        await plugin.ConnectionManager.RequestPeriodicAdscContract(
            request.Callsign,
            request.ReportingInterval,
            cancellationToken);
        logger.Information(
            "Selected ADS-C reporting interval {ReportingInterval} for {Callsign}",
            request.ReportingInterval,
            request.Callsign);
    }
}
