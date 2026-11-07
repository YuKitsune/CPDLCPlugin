using CPDLCServer.Infrastructure;
using CPDLCServer.Settings;

namespace CPDLCServer.Cpdlc.Connections;

public class LostConnectionService : IHostedService, IDisposable
{
    readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    readonly IUnitOfWorkMediator _mediator;
    readonly ILogger _logger;

    CancellationTokenSource? _cancellationTokenSource;
    Task? _task;

    public LostConnectionService(IUnitOfWorkMediator mediator, ILogger logger)
    {
        _mediator = mediator;
        _logger = logger.ForContext<LostConnectionService>();
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
        _logger.Information("Starting lost connection service");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.Debug("Checking for lost connections");

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    timeoutCts.CancelAfter(TimeSpan.FromMinutes(1));

                    var settings = await _mediator.Send(new GetServerSettingsRequest(), timeoutCts.Token);
                    var connectionTimeout = TimeSpan.FromMinutes(settings.AircraftConnectionTimeoutMinutes);
                    await _mediator.Send(new CheckLostAircraftConnectionsRequest(connectionTimeout), timeoutCts.Token);

                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error checking for lost connections");
                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected on shutdown
        }
        catch (Exception exception)
        {
            _logger.Fatal(exception, "LostConnectionService failed");
        }

        _logger.Information("Stopped lost connection service");
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        _task?.Dispose();
    }
}
