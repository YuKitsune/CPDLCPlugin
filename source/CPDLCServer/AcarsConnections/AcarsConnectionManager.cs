using System.Collections.Concurrent;
using CPDLCServer.AcarsConnections.ConnectionSettings;
using CPDLCServer.AcarsConnections.Hoppie;
using CPDLCServer.AdscContracts;
using CPDLCServer.AdscContracts.MessageLog;
using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using CPDLCServer.Security;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.AcarsConnections;

public interface IAcarsConnectionManager
{
    Task<IAcarsConnection> GetAcarsConnection(string acarsConnectionId, CancellationToken cancellationToken);
    Task<IAcarsConnection> GetAcarsConnectionForStation(string stationId, CancellationToken cancellationToken);
    Task<AcarsConfiguration> GetConfigurationForStation(string stationId, CancellationToken cancellationToken);
    Task<string?> FindAcarsConnectionId(string stationId, CancellationToken cancellationToken);

    // The IDs of the connections that are running
    string[] GetAcarsConnectionIds();
    bool IsRunning(Guid settingsId);

    // Connects to the upstream ACARS network
    Task Start(AcarsConnectionSettings settings, CancellationToken cancellationToken);

    // Disconnects from the upstream ACARS network. Does nothing if the connection is not running.
    Task Stop(Guid settingsId, CancellationToken cancellationToken);
}

public class AcarsConnectionManager(
    IServiceScopeFactory serviceScopeFactory,
    MasterKeyEncryption masterKeyEncryption,
    IUnitOfWorkMediator mediator,
    IClock clock,
    IAdscMessageLogRepository adscMessageLogRepository,
    ILogger logger)
    : BackgroundService, IAcarsConnectionManager
{
    readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    readonly ConcurrentDictionary<string, AcarsConnectionHandle> _connections = [];
    readonly ILogger _logger = logger.ForContext<AcarsConnectionManager>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabledSettings = await LoadEnabledSettings(stoppingToken);
        _logger.Information("Starting ACARS connection manager with {Count} enabled connections", enabledSettings.Length);

        foreach (var settings in enabledSettings)
        {
            await StartWithRetry(settings, stoppingToken);
        }

        _logger.Information("All ACARS connections initialized");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.Information("Stopping ACARS connection manager");
        }
    }

    async Task<AcarsConnectionSettings[]> LoadEnabledSettings(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CpdlcDbContext>();

        return await dbContext.AcarsConnections
            .AsNoTracking()
            .Where(s => s.Enabled)
            .ToArrayAsync(cancellationToken);
    }

    async Task StartWithRetry(AcarsConnectionSettings settings, CancellationToken cancellationToken)
    {
        const int maxRetries = 5;
        var retryDelay = TimeSpan.FromSeconds(1);

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await Start(settings, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxRetries - 1)
            {
                _logger.Warning(
                    ex,
                    "Failed to start ACARS connection for {StationId} (attempt {Attempt}/{MaxRetries}). Retrying in {Delay}",
                    settings.StationIdentifier,
                    attempt + 1,
                    maxRetries,
                    retryDelay);

                await Task.Delay(retryDelay, cancellationToken);
                retryDelay *= 2;
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "Failed to start ACARS connection for {StationId} after {MaxRetries} attempts. The connection will not be available",
                    settings.StationIdentifier,
                    maxRetries);
                break;
            }
        }
    }

    public async Task Start(AcarsConnectionSettings settings, CancellationToken cancellationToken)
    {
        using (await _semaphoreSlim.LockAsync(cancellationToken))
        {
            var configuration = CreateConfiguration(settings);
            if (_connections.ContainsKey(configuration.AcarsConnectionId))
                throw new InvalidOperationException($"ACARS connection {configuration.AcarsConnectionId} is already running");

            var acarsConnectionHandle = await CreateAcarsConnection(settings.Id, configuration, cancellationToken);
            _connections[configuration.AcarsConnectionId] = acarsConnectionHandle;

            _logger.Information("Started ACARS connection {AcarsConnectionId}", configuration.AcarsConnectionId);
        }
    }

    public async Task Stop(Guid settingsId, CancellationToken cancellationToken)
    {
        using (await _semaphoreSlim.LockAsync(cancellationToken))
        {
            var (acarsConnectionId, acarsConnectionHandle) = _connections.FirstOrDefault(c => c.Value.SettingsId == settingsId);
            if (acarsConnectionHandle is null)
                return;

            _connections.TryRemove(acarsConnectionId, out _);
            await acarsConnectionHandle.DisposeAsync();

            _logger.Information("Stopped ACARS connection {AcarsConnectionId}", acarsConnectionId);
        }
    }

    public string[] GetAcarsConnectionIds()
    {
        return _connections.Keys.ToArray();
    }

    public bool IsRunning(Guid settingsId)
    {
        return _connections.Values.Any(h => h.SettingsId == settingsId);
    }

    public Task<IAcarsConnection> GetAcarsConnection(string acarsConnectionId, CancellationToken cancellationToken)
    {
        if (!_connections.TryGetValue(acarsConnectionId, out var acarsConnectionHandle))
        {
            throw new ConfigurationNotFoundException(acarsConnectionId);
        }

        return Task.FromResult(acarsConnectionHandle.Connection);
    }

    public Task<IAcarsConnection> GetAcarsConnectionForStation(string stationId, CancellationToken cancellationToken)
    {
        var acarsConnectionHandle = _connections.Values.FirstOrDefault(h => h.Connection.StationId == stationId);
        if (acarsConnectionHandle is null)
        {
            throw new Exception($"No ACARS connection found for station '{stationId}'");
        }

        return Task.FromResult(acarsConnectionHandle.Connection);
    }

    public Task<AcarsConfiguration> GetConfigurationForStation(string stationId, CancellationToken cancellationToken)
    {
        var acarsConnectionHandle = _connections.Values.FirstOrDefault(h => h.Connection.StationId == stationId);
        if (acarsConnectionHandle is null)
        {
            throw new Exception($"No ACARS configuration found for station '{stationId}'");
        }

        return Task.FromResult(acarsConnectionHandle.Configuration);
    }

    public Task<string?> FindAcarsConnectionId(string stationId, CancellationToken cancellationToken)
    {
        var acarsConnectionId = _connections
            .Where(c => string.Equals(c.Value.Connection.StationId, stationId, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Key)
            .FirstOrDefault();

        return Task.FromResult(acarsConnectionId);
    }

    AcarsConfiguration CreateConfiguration(AcarsConnectionSettings settings)
    {
        return settings.Type switch
        {
            AcarsNetworkType.Hoppie => new HoppiesConfiguration
            {
                Url = settings.Url,
                AuthenticationCode = masterKeyEncryption.Decrypt(settings.EncryptedAuthenticationCode),
                FlightSimulationNetwork = settings.FlightSimulationNetwork,
                StationIdentifier = settings.StationIdentifier,
                DefaultAdscReportingInterval = settings.DefaultAdscReportingInterval,
                SelectedAdscContractDuration = settings.SelectedAdscContractDuration
            },
            _ => throw new NotSupportedException($"ACARS network type {settings.Type} is not supported")
        };
    }

    async Task<AcarsConnectionHandle> CreateAcarsConnection(
        Guid settingsId,
        AcarsConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var acarsConnectionId = configuration.AcarsConnectionId;

        IAcarsConnection acarsConnection = configuration switch
        {
            HoppiesConfiguration hoppieConfig => CreateHoppieConnection(hoppieConfig),
            _ => throw new NotSupportedException($"ACARS configuration type {configuration.GetType().Name} is not supported")
        };

        var stationId = acarsConnection.StationId;

        var subscribeTaskCancellationSource = new CancellationTokenSource();
        var subscribeTask = Task.WhenAll(
            Subscribe(
                acarsConnectionId,
                stationId,
                acarsConnection,
                mediator,
                subscribeTaskCancellationSource.Token),
            SubscribeAdsc(
                acarsConnectionId,
                stationId,
                acarsConnection,
                mediator,
                subscribeTaskCancellationSource.Token));

        var acarsConnectionHandle = new AcarsConnectionHandle(
            settingsId,
            configuration,
            acarsConnection,
            subscribeTask,
            subscribeTaskCancellationSource);

        await acarsConnection.Connect(cancellationToken);

        _logger.Information("Connected to ACARS connection {AcarsConnectionId}", acarsConnectionId);

        return acarsConnectionHandle;
    }

    HoppieAcarsConnection CreateHoppieConnection(HoppiesConfiguration configuration)
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = configuration.Url;

        return new HoppieAcarsConnection(
            configuration,
            httpClient,
            clock,
            adscMessageLogRepository,
            _logger.ForContext<HoppieAcarsConnection>());
    }

    async Task Subscribe(string acarsConnectionId, string stationId, IAcarsConnection acarsConnection, IUnitOfWorkMediator mediator, CancellationToken cancellationToken)
    {
        var subscriptionLogger = _logger.ForContext("AcarsConnectionId", acarsConnectionId);

        try
        {
            await foreach (var downlinkMessage in acarsConnection.MessageReader.ReadAllAsync(cancellationToken))
            {
                // TODO: Make this configurable
                // Linked to the subscription, so that a stopped connection does not wait for the timeout
                using var publishTimeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                publishTimeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));

                try
                {
                    await mediator.Publish(
                        new DownlinkReceivedNotification(
                            acarsConnectionId,
                            stationId,
                            downlinkMessage),
                        publishTimeoutCancellationTokenSource.Token);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    subscriptionLogger.Information("Subscription canceled");
                    return;
                }
                catch (OperationCanceledException) when (publishTimeoutCancellationTokenSource.Token.IsCancellationRequested)
                {
                    subscriptionLogger.Warning("Timeout handling downlink {Downlink}", downlinkMessage);
                }
                catch (Exception ex)
                {
                    subscriptionLogger.Error(ex, "Failed to relay {MessageType}", downlinkMessage.GetType());
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            subscriptionLogger.Information("Subscription canceled");
        }
    }

    async Task SubscribeAdsc(string acarsConnectionId, string stationId, IAcarsConnection acarsConnection, IUnitOfWorkMediator mediator, CancellationToken cancellationToken)
    {
        var subscriptionLogger = _logger.ForContext("AcarsConnectionId", acarsConnectionId);

        try
        {
            await foreach (var adscMessage in acarsConnection.AdscMessageReader.ReadAllAsync(cancellationToken))
            {
                // TODO: Make this configurable
                // Linked to the subscription, so that a stopped connection does not wait for the timeout
                using var publishTimeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                publishTimeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));

                try
                {
                    await mediator.Publish(
                        new AdscMessageReceivedNotification(
                            acarsConnectionId,
                            stationId,
                            adscMessage),
                        publishTimeoutCancellationTokenSource.Token);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    subscriptionLogger.Information("ADS-C subscription canceled");
                    return;
                }
                catch (OperationCanceledException) when (publishTimeoutCancellationTokenSource.Token.IsCancellationRequested)
                {
                    subscriptionLogger.Warning("Timeout handling ADS-C message {AdscMessage}", adscMessage);
                }
                catch (Exception ex)
                {
                    subscriptionLogger.Error(ex, "Failed to relay {MessageType}", adscMessage.GetType());
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            subscriptionLogger.Information("ADS-C subscription canceled");
        }
    }

    record AcarsConnectionHandle(
        Guid SettingsId,
        AcarsConfiguration Configuration,
        IAcarsConnection Connection,
        Task SubscribeTask,
        CancellationTokenSource SubscribeCancellationTokenSource) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Connection.DisposeAsync();

            await SubscribeCancellationTokenSource.CancelAsync();

            try
            {
                await SubscribeTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }

            SubscribeCancellationTokenSource.Dispose();
            SubscribeTask.Dispose();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Information("Stopping ACARS connection manager and disposing all connections");

        await base.StopAsync(cancellationToken);

        foreach (var (_, connectionHandle) in _connections)
        {
            await connectionHandle.DisposeAsync();
        }

        _connections.Clear();
        _semaphoreSlim.Dispose();
    }
}
