using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record DisconnectRequest : IRequest;

public class DisconnectRequestHandler(Plugin plugin, IPublisher publisher, ILogger logger) : IRequestHandler<DisconnectRequest>
{
    public async Task Handle(DisconnectRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Disconnecting from server");

        if (plugin.ConnectionManager is null)
        {
            logger.Warning("No connection manager found, already disconnected");
            return;
        }

        if (plugin.ConnectionManager.IsConnected)
        {
            logger.Verbose("Stopping active connection");
            await plugin.ConnectionManager.StopAsync();
        }

        plugin.ConnectionManager.Dispose();
        plugin.ConnectionManager = null;

        logger.Verbose("Disconnected from server");
        await publisher.Publish(new DisconnectedNotification(), cancellationToken);
    }
}
