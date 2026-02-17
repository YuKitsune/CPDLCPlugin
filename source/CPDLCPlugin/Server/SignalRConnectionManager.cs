using CPDLCServer.Contracts;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CPDLCPlugin.Server;

/// <summary>
/// Manages the SignalR connection to the CPDLC server.
/// </summary>
public class SignalRConnectionManager(
    string serverEndpoint,
    IDownlinkHandlerDelegate downlinkHandlerDelegate,
    ILogger logger)
    : IDisposable
{
    HubConnection? _connection;
    bool _isDisposed;

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    public HubConnectionState ConnectionState => _connection?.State ?? HubConnectionState.Disconnected;

    /// <summary>
    /// Gets whether the connection is currently active.
    /// </summary>
    public bool IsConnected => ConnectionState == HubConnectionState.Connected;

    /// <summary>
    /// Event raised when the connection state changes.
    /// </summary>
    public event EventHandler<HubConnectionState>? ConnectionStateChanged;

    /// <summary>
    /// Event raised when a connection error occurs.
    /// </summary>
    public event EventHandler<Exception>? ConnectionError;

    public string ServerEndpoint { get; } = serverEndpoint;

    /// <summary>
    /// Initializes the SignalR connection.
    /// </summary>
    public async Task InitializeAsync(string callsign)
    {
        if (_connection != null)
        {
            logger.Debug("Disposing existing connection before re-initialization");
            await DisposeConnectionAsync();
        }

        var url = $"{ServerEndpoint}?callsign={callsign}";
        logger.Debug("Building SignalR connection to {Url}", url);

        var hubConnectionBuilder = new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
#if DEBUG
                // In DEBUG mode, bypass SSL certificate validation for local development
                options.HttpMessageHandlerFactory = _ =>
                {
                    var handler = new System.Net.Http.HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };
                    return handler;
                };
#endif
            })
            .AddJsonProtocol()
            .WithAutomaticReconnect();

        _connection = hubConnectionBuilder.Build();

        RegisterHandlers();
        RegisterConnectionEvents();
    }

    /// <summary>
    /// Starts the connection to the server.
    /// </summary>
    public async Task StartAsync()
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Connection not initialized. Call InitializeAsync first.");
        }

        if (_connection.State == HubConnectionState.Connected)
        {
            logger.Warning("Connection already active, skipping start");
            return;
        }

        try
        {
            await _connection.StartAsync();
            OnConnectionStateChanged(HubConnectionState.Connected);
        }
        catch (Exception ex)
        {
            OnConnectionError(ex);
            throw;
        }
    }

    /// <summary>
    /// Stops the connection to the server.
    /// </summary>
    public async Task StopAsync()
    {
        if (_connection == null || _connection.State == HubConnectionState.Disconnected)
        {
            logger.Warning("Connection already stopped or not initialized, skipping stop");
            return;
        }

        try
        {
            await _connection.StopAsync();
            OnConnectionStateChanged(HubConnectionState.Disconnected);
        }
        catch (Exception ex)
        {
            OnConnectionError(ex);
            throw;
        }
    }

    void RegisterHandlers()
    {
        if (_connection == null) return;

        logger.Debug("Registering SignalR message handlers");
        _connection.On<DialogueDto>("DialogueChanged", downlink =>
            WithCancellationToken<DialogueDto>(downlinkHandlerDelegate.DialogueChanged)(downlink));
        _connection.On<AircraftConnectionDto>("AircraftConnectionUpdated", connectedAircraftInfo =>
            WithCancellationToken<AircraftConnectionDto>(downlinkHandlerDelegate.AircraftConnectionUpdated)(connectedAircraftInfo));
        _connection.On<string>("AircraftConnectionRemoved", callsign =>
            WithCancellationToken<string>(downlinkHandlerDelegate.AircraftConnectionRemoved)(callsign));
        _connection.On<ControllerConnectionDto>("ControllerConnectionUpdated", connectedAircraftInfo =>
            WithCancellationToken<ControllerConnectionDto>(downlinkHandlerDelegate.ControllerConnectionUpdated)(connectedAircraftInfo));
        _connection.On<string>("ControllerConnectionRemoved", callsign =>
            WithCancellationToken<string>(downlinkHandlerDelegate.ControllerConnectionRemoved)(callsign));
    }

    Func<T, Task> WithCancellationToken<T>(Func<T, CancellationToken, Task> action)
    {
        // TODO: Make configurable
        return async x =>
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await action(x, cancellationTokenSource.Token);
        };
    }

    public async Task<UplinkMessageDto> SendUplink(
        string recipient,
        int? replyToDownlinkId,
        CpdlcUplinkResponseType responseType,
        string content,
        CancellationToken cancellationToken)
    {
        EnsureConnected();
        var result = await _connection!.InvokeAsync<UplinkMessageDto>(
            "SendUplink",
            recipient,
            replyToDownlinkId,
            responseType,
            content,
            cancellationToken: cancellationToken);

        return result;
    }

    public async Task<AircraftConnectionDto[]> GetConnectedAircraft(CancellationToken cancellationToken)
    {
        EnsureConnected();
        var aircraft = await _connection!.InvokeAsync<AircraftConnectionDto[]>(
            "GetConnectedAircraft",
            cancellationToken);

        return aircraft;
    }

    public async Task<ControllerConnectionDto[]> GetConnectedControllers(CancellationToken cancellationToken)
    {
        EnsureConnected();
        var controllers = await _connection!.InvokeAsync<ControllerConnectionDto[]>(
            "GetConnectedControllers",
            cancellationToken);

        return controllers;
    }

    public async Task AcknowledgeDownlink(Guid dialogueId, int downlinkMessageId, CancellationToken cancellationToken)
    {
        EnsureConnected();
        await _connection!.InvokeAsync(
            "AcknowledgeDownlink",
            dialogueId,
            downlinkMessageId,
            cancellationToken);
    }

    public async Task AcknowledgeUplink(Guid dialogueId, int uplinkMessageId, CancellationToken cancellationToken)
    {
        EnsureConnected();
        await _connection!.InvokeAsync(
            "AcknowledgeUplink",
            dialogueId,
            uplinkMessageId,
            cancellationToken);
    }

    public async Task ArchiveDialogue(Guid dialogueId, CancellationToken cancellationToken)
    {
        EnsureConnected();
        await _connection!.InvokeAsync("ArchiveDialogue", dialogueId, cancellationToken);
    }

    public async Task<DialogueDto[]> GetAllDialogues(CancellationToken cancellationToken)
    {
        EnsureConnected();
        var dialogues = await _connection!.InvokeAsync<DialogueDto[]>(
            "GetAllDialogues",
            cancellationToken);

        return dialogues;
    }

    /// <summary>
    /// Registers connection lifecycle event handlers.
    /// </summary>
    void RegisterConnectionEvents()
    {
        if (_connection == null) return;

        logger.Debug("Registering SignalR connection lifecycle event handlers");

        _connection.Closed += async error =>
        {
            if (error != null)
            {
                logger.Warning(error, "SignalR connection closed with error");
                OnConnectionError(error);
            }
            else
            {
                logger.Debug("SignalR connection closed");
            }

            OnConnectionStateChanged(HubConnectionState.Disconnected);
            await Task.CompletedTask;
        };

        _connection.Reconnecting += error =>
        {
            if (error != null)
            {
                logger.Warning(error, "SignalR connection lost, attempting to reconnect");
                OnConnectionError(error);
            }
            else
            {
                logger.Information("SignalR connection reconnecting");
            }

            OnConnectionStateChanged(HubConnectionState.Reconnecting);
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            logger.Information("SignalR connection reconnected successfully (ConnectionId: {ConnectionId})", connectionId);
            OnConnectionStateChanged(HubConnectionState.Connected);
            return Task.CompletedTask;
        };
    }

    void OnConnectionStateChanged(HubConnectionState newState)
    {
        logger.Debug("Connection state changed to {State}", newState);
        ConnectionStateChanged?.Invoke(this, newState);
    }

    void OnConnectionError(Exception error)
    {
        logger.Error(error, "SignalR connection error occurred");
        ConnectionError?.Invoke(this, error);
        downlinkHandlerDelegate.Error(error);
    }

    /// <summary>
    /// Ensures the connection is active before attempting operations.
    /// </summary>
    void EnsureConnected()
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Connection not initialized. Call InitializeAsync first.");
        }

        if (_connection.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException($"Connection is not active. Current state: {_connection.State}");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        logger.Debug("Disposing SignalR connection manager");
        DisposeConnectionAsync().GetAwaiter().GetResult();
        _isDisposed = true;
        logger.Debug("SignalR connection manager disposed");
        GC.SuppressFinalize(this);
    }

    async Task DisposeConnectionAsync()
    {
        if (_connection != null)
        {
            try
            {
                logger.Debug("Stopping connection during disposal");
                await _connection.StopAsync();
            }
            catch (Exception ex)
            {
                logger.Debug(ex, "Error stopping connection during disposal (ignoring)");
                // Ignore errors during disposal
            }
            finally
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
    }
}
