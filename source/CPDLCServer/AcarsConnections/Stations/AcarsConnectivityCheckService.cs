using CPDLCServer.Infrastructure;
using CPDLCServer.Settings;

namespace CPDLCServer.AcarsConnections.Stations;

public class AcarsConnectivityCheckService : IHostedService, IDisposable
{
#if DEBUG
    readonly TimeSpan _errorInterval = TimeSpan.FromSeconds(5);
#else
    readonly TimeSpan _errorInterval = TimeSpan.FromMinutes(5);
#endif

    readonly IAcarsConnectionManager _acarsConnectionManager;
    readonly IUnitOfWorkMediator _mediator;
    readonly IServiceScopeFactory _serviceScopeFactory;
    readonly ILogger _logger;

    CancellationTokenSource? _cancellationTokenSource;
    Task? _task;

    public AcarsConnectivityCheckService(
        IAcarsConnectionManager acarsConnectionManager,
        IUnitOfWorkMediator mediator,
        IServiceScopeFactory serviceScopeFactory,
        ILogger logger)
    {
        _acarsConnectionManager = acarsConnectionManager;
        _mediator = mediator;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger.ForContext<AcarsConnectivityCheckService>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource is not null || _task is not null)
            throw new Exception("Already started");

        _cancellationTokenSource = new CancellationTokenSource();
        _task = DoWork(_cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource is null || _task is null)
            throw new Exception("Already stopped");

        await _cancellationTokenSource.CancelAsync();
        await _task;

        _cancellationTokenSource = null;
        _task = null;
    }

    async Task DoWork(CancellationToken stoppingToken)
    {
        _logger.Information("Starting aircraft connectivity check service");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.Debug("Checking aircraft connectivity");

                    // The connections can start and stop while the server is running, so get them each time
                    var acarsConnectionIds = _acarsConnectionManager.GetAcarsConnectionIds();
                    if (acarsConnectionIds.Length == 0)
                    {
                        _logger.Debug("No ACARS connections are running, retrying");
                        await Task.Delay(_errorInterval, stoppingToken);
                        continue;
                    }

                    foreach (var acarsConnectionId in acarsConnectionIds)
                    {
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        timeoutCts.CancelAfter(TimeSpan.FromMinutes(1));

                        await _mediator.Send(
                            new RefreshAcarsCallsignsRequest(acarsConnectionId),
                            timeoutCts.Token);
                    }

                    var settings = await LoadSettings(stoppingToken);
                    var checkInterval = TimeSpan.FromMinutes(settings.ConnectivityCheckIntervalMinutes);
                    await Task.Delay(checkInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ConfigurationNotFoundException)
                {
                    _logger.Debug("ACARS connection not yet available, retrying");
                    await Task.Delay(_errorInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error checking aircraft connectivity");
                    await Task.Delay(_errorInterval, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected on shutdown
        }
        catch (Exception exception)
        {
            _logger.Fatal(exception, "AcarsConnectivityCheckService failed");
        }

        _logger.Information("Stopped aircraft connectivity check service");
    }

    // The settings provider is scoped, and this service is a singleton
    async Task<ServerSettings> LoadSettings(CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var settingsProvider = scope.ServiceProvider.GetRequiredService<IServerSettingsProvider>();
        return await settingsProvider.Load(cancellationToken);
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        _task?.Dispose();
    }
}
