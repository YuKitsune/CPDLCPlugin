using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record RequestAdscDemandReportRequest(string Callsign) : IRequest;

public class RequestAdscDemandReportRequestHandler(
    Plugin plugin,
    ILogger logger)
    : IRequestHandler<RequestAdscDemandReportRequest>
{
    public async Task Handle(RequestAdscDemandReportRequest request, CancellationToken cancellationToken)
    {
        logger.Verbose("Requesting ADS-C demand report for {Callsign}", request.Callsign);
        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning("Cannot request ADS-C demand report for {Callsign}: not connected to server", request.Callsign);
            return;
        }

        await plugin.ConnectionManager.RequestAdscDemandReport(request.Callsign, cancellationToken);
        logger.Information("Requested ADS-C demand report for {Callsign}", request.Callsign);
    }
}
