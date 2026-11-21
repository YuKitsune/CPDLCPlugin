using CPDLCPlugin.Server;
using MediatR;
using Serilog;
using vatsys;
using vatSysToolbox;

namespace CPDLCPlugin.Messages;

public record ConnectRequest(string ServerEndpoint, string StationId) : IRequest;

public class ConnectRequestHandler(Plugin plugin, IMediator mediator, IErrorReporter errorReporter, ILogger logger) : IRequestHandler<ConnectRequest>
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

        // The CPDLC server is for the live network only
        if (!Network.IsOfficialServer)
        {
            logger.Warning("Cannot connect to server: connected to the sweatbox");
            throw new Exception("Cannot connect to the CPDLC server on the sweatbox");
        }

        // Check the token first, so that an expired token gives a clear error
        var token = await mediator.Send(new CheckAuthenticationRequest(request.ServerEndpoint), cancellationToken);
        if (token is null)
        {
            logger.Warning("Cannot connect to server: not authenticated");
            throw new Exception("Not authenticated with the CPDLC server. Open SETUP and select Authenticate.");
        }

        logger.Verbose("Creating SignalR connection");
        var downlinkHandler = new MediatorMessageHandler(mediator, errorReporter);
        plugin.ConnectionManager = new SignalRConnectionManager(
            request.ServerEndpoint,
            token,
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
