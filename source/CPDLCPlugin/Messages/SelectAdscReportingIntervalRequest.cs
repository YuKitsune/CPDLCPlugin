using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

// A null reporting interval ends the selected contract and requests the default contract again
public record SelectAdscReportingIntervalRequest(string Callsign, TimeSpan? ReportingInterval) : IRequest;

public class SelectAdscReportingIntervalRequestHandler(
    Plugin plugin,
    ILogger logger)
    : IRequestHandler<SelectAdscReportingIntervalRequest>
{
    public async Task Handle(SelectAdscReportingIntervalRequest request, CancellationToken cancellationToken)
    {
        logger.Verbose(
            "Selecting ADS-C reporting interval {ReportingInterval} for {Callsign}",
            request.ReportingInterval?.ToString() ?? "Default",
            request.Callsign);
        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning("Not connected to server");
            return;
        }

        await plugin.ConnectionManager.RequestPeriodicAdscContract(
            request.Callsign,
            request.ReportingInterval,
            cancellationToken);
        logger.Information(
            "Selected ADS-C reporting interval {ReportingInterval} for {Callsign}",
            request.ReportingInterval?.ToString() ?? "Default",
            request.Callsign);
    }
}
