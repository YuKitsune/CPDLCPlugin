using CPDLCPlugin.Server;
using MediatR;
using Serilog;
using vatsys;

namespace CPDLCPlugin.Messages;

public record ConnectRequest(string ServerEndpoint, string StationId) : IRequest;

public class ConnectRequestHandler(Plugin plugin, IMediator mediator, ILogger logger) : IRequestHandler<ConnectRequest>
{
    public async Task Handle(ConnectRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Connecting to {ServerEndpoint}", request.ServerEndpoint);

        // If already connected, disconnect first
        if (plugin.ConnectionManager is not null)
        {
            logger.Information("Existing connection found, disconnecting");
            if (plugin.ConnectionManager.IsConnected)
            {
                await plugin.ConnectionManager.StopAsync();
                logger.Information("Existing connection stopped");
            }

            plugin.ConnectionManager.Dispose();
            plugin.ConnectionManager = null;
        }

        if (!Network.IsConnected)
        {
            logger.Warning("Cannot connect to server: not connected to VATSIM");
            throw new Exception("Not connected to VATSIM");
        }

        logger.Verbose("Creating SignalR connection");
        var downlinkHandler = new MediatorMessageHandler(mediator);
        plugin.ConnectionManager = new SignalRConnectionManager(
            request.ServerEndpoint,
            downlinkHandler,
            logger.ForContext<SignalRConnectionManager>());

        // Initialize the connection with the station ID and current callsign
        logger.Information("Initializing SignalR connection");
        await plugin.ConnectionManager.InitializeAsync(request.StationId, Network.Callsign);

        // Start the connection
        logger.Information("Starting SignalR connection");
        await plugin.ConnectionManager.StartAsync();

        logger.Information("Connected to server");
        await mediator.Publish(new ConnectedNotification(), cancellationToken);
    }
}
