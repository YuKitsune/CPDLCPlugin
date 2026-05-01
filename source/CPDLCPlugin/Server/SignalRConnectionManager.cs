using CPDLCServer.Contracts;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CPDLCPlugin.Server;

// TODO: Register this in the IoC container

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
    int _isDisposed;
    bool _isStopping;

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    public HubConnectionState ConnectionState => _connection?.State ?? HubConnectionState.Disconnected;

    /// <summary>
    /// Gets whether the connection is currently active.
    /// </summary>
    public bool IsConnected => ConnectionState == HubConnectionState.Connected;

    public string ServerEndpoint { get; } = serverEndpoint;
    public string StationIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Initializes the SignalR connection.
    /// </summary>
    public async Task InitializeAsync(string stationId, string callsign)
    {
        if (_connection != null)
        {
            logger.Verbose("Disposing existing connection before re-initialization");
            await DisposeConnectionAsync();
        }

        var url = $"{ServerEndpoint}?callsign={callsign}&stationId={stationId}";
        logger.Verbose("Building SignalR connection to {Url}", url);

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

        StationIdentifier = stationId;
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

        await _connection.StartAsync();
        OnConnectionStateChanged(HubConnectionState.Connected);
    }

    /// <summary>
    /// Stops the connection to the server.
    /// </summary>
    public async Task StopAsync()
    {
        var connection = _connection;
        if (connection == null || connection.State == HubConnectionState.Disconnected)
        {
            logger.Warning("Connection already stopped or not initialized, skipping stop");
            return;
        }

        await connection.StopAsync();
        OnConnectionStateChanged(HubConnectionState.Disconnected);
    }

    void RegisterHandlers()
    {
        if (_connection == null) return;

        logger.Verbose("Registering SignalR message handlers");
        _connection.On<DialogueDto>("DialogueChanged", downlink =>
            WithCancellationToken<DialogueDto>(downlinkHandlerDelegate.DialogueChanged)(downlink));
        _connection.On<AircraftConnectionDto>("AircraftConnectionUpdated", connectedAircraftInfo =>
            WithCancellationToken<AircraftConnectionDto>(downlinkHandlerDelegate.AircraftConnectionUpdated)(connectedAircraftInfo));
        _connection.On<string, string>("AircraftConnectionRemoved", (callsign, stationId) =>
            WithCancellationToken<string, string>(downlinkHandlerDelegate.AircraftConnectionRemoved)(callsign, stationId));
        _connection.On<string[]>("AcarsConnectedCallsignsUpdated", callsigns =>
            WithCancellationToken<string[]>(downlinkHandlerDelegate.AcarsConnectedCallsignsUpdated)(callsigns));
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

    Func<T1, T2, Task> WithCancellationToken<T1, T2>(Func<T1, T2, CancellationToken, Task> action)
    {
        // TODO: Make configurable
        return async (x, y) =>
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await action(x, y, cancellationTokenSource.Token);
        };
    }

    public async Task<UplinkMessageDto> SendUplink(
        string recipient,
        int? replyToDownlinkId,
        CpdlcUplinkResponseType responseType,
        string content,
        CancellationToken cancellationToken)
    {
        var connection = GetConnectedOrThrow();
        return await connection.InvokeAsync<UplinkMessageDto>(
            "SendUplink",
            recipient,
            replyToDownlinkId,
            responseType,
            content,
            cancellationToken: cancellationToken);
    }

    public async Task<AircraftConnectionDto[]> GetConnectedAircraft(CancellationToken cancellationToken)
    {
        var connection = GetConnectedOrThrow();
        return await connection.InvokeAsync<AircraftConnectionDto[]>(
            "GetConnectedAircraft",
            cancellationToken);
    }

    public async Task<string[]> GetAcarsConnectedCallsigns(CancellationToken cancellationToken)
    {
        var connection = GetConnectedOrThrow();
        return await connection.InvokeAsync<string[]>(
            "GetAcarsConnectedCallsigns",
            cancellationToken);
    }

    public async Task AcknowledgeDownlink(Guid dialogueId, int downlinkMessageId, CancellationToken cancellationToken)
    {
        var connection = GetConnectedOrThrow();
        await connection.InvokeAsync(
            "AcknowledgeDownlink",
            dialogueId,
            downlinkMessageId,
            cancellationToken);
    }

    public async Task AcknowledgeUplink(Guid dialogueId, int uplinkMessageId, CancellationToken cancellationToken)
    {
        var connection = GetConnectedOrThrow();
        await connection.InvokeAsync(
            "AcknowledgeUplink",
            dialogueId,
            uplinkMessageId,
            cancellationToken);
    }

    public async Task ArchiveDialogue(Guid dialogueId, CancellationToken cancellationToken)
    {
        var connection = GetConnectedOrThrow();
        await connection.InvokeAsync("ArchiveDialogue", dialogueId, cancellationToken);
    }

    public async Task UpdateNextDataAuthority(
        string callsign,
        string? nextDataAuthority,
        DateTimeOffset? expectedTransferTime,
        CancellationToken cancellationToken)
    {
        var connection = GetConnectedOrThrow();
        await connection.InvokeAsync(
            "UpdateNextDataAuthority",
            callsign,
            nextDataAuthority,
            expectedTransferTime,
            cancellationToken: cancellationToken);
    }

    public async Task<DialogueDto[]> GetAllDialogues(CancellationToken cancellationToken)
    {
        var connection = GetConnectedOrThrow();
        return await connection.InvokeAsync<DialogueDto[]>(
            "GetAllDialogues",
            cancellationToken);
    }

    /// <summary>
    /// Registers connection lifecycle event handlers.
    /// </summary>
    void RegisterConnectionEvents()
    {
        if (_connection == null) return;

        logger.Verbose("Registering SignalR connection lifecycle event handlers");

        _connection.Closed += error =>
        {
            if (error != null)
            {
                if (_isStopping)
                {
                    logger.Verbose(error, "SignalR connection closed with error during intentional stop");
                }
                else
                {
                    logger.Warning(error, "SignalR connection closed with error");
                    OnConnectionError(error);
                }
            }
            else
            {
                logger.Verbose("SignalR connection closed");
            }

            OnConnectionStateChanged(HubConnectionState.Disconnected);
            return Task.CompletedTask;
        };

        _connection.Reconnecting += error =>
        {
            if (error != null)
            {
                logger.Warning(error, "SignalR connection lost, attempting to reconnect");
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
        logger.Verbose("Connection state changed to {State}", newState);
    }

    void OnConnectionError(Exception error)
    {
        logger.Error(error, "SignalR connection error occurred");
        downlinkHandlerDelegate.Error(error);
    }

    HubConnection GetConnectedOrThrow()
    {
        var connection = _connection ?? throw new InvalidOperationException("Connection not initialized. Call InitializeAsync first.");
        if (connection.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException($"Connection is not active. Current state: {connection.State}");
        }

        return connection;
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;

        logger.Verbose("Disposing SignalR connection manager");
        DisposeConnectionAsync().GetAwaiter().GetResult();
        logger.Verbose("SignalR connection manager disposed");
        GC.SuppressFinalize(this);
    }

    async Task DisposeConnectionAsync()
    {
        if (_connection != null)
        {
            try
            {
                if (_connection.State != HubConnectionState.Disconnected)
                {
                    logger.Verbose("Stopping connection during disposal");
                    _isStopping = true;
                    await _connection.StopAsync();
                }
            }
            catch (Exception ex)
            {
                logger.Verbose(ex, "Error stopping connection during disposal (ignoring)");
                // Ignore errors during disposal
            }
            finally
            {
                _isStopping = false;
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
    }
}
