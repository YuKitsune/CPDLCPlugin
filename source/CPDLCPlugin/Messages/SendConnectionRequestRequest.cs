using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record SendConnectionRequestRequest(string Callsign, string StationId) : IRequest;

public class SendConnectionRequestRequestHandler(Plugin plugin, ILogger logger)
    : IRequestHandler<SendConnectionRequestRequest>
{
    public async Task Handle(SendConnectionRequestRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Sending connection request for {StationId} to {Callsign}", request.StationId, request.Callsign);

        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning("Not connected to server");
            return;
        }

        await plugin.ConnectionManager.RequestConnection(
            request.Callsign,
            request.StationId,
            cancellationToken);
    }
}
