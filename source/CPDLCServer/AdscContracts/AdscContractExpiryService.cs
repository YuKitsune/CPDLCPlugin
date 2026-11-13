using CPDLCServer.Infrastructure;

namespace CPDLCServer.AdscContracts;

public class AdscContractExpiryService(IUnitOfWorkMediator mediator, ILogger logger) : IHostedService, IDisposable
{
    readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    readonly ILogger _logger = logger.ForContext<AdscContractExpiryService>();

    CancellationTokenSource? _cancellationTokenSource;
    Task? _task;

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
        _logger.Information("Starting ADS-C contract expiry service");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.Debug("Checking for ended ADS-C contracts and expired ADS-C data");

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    timeoutCts.CancelAfter(TimeSpan.FromMinutes(1));

                    await mediator.Send(new EndExpiredAdscContractsRequest(), timeoutCts.Token);
                    await mediator.Send(new PurgeExpiredAdscDataRequest(), timeoutCts.Token);

                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error checking for ended ADS-C contracts and expired ADS-C data");
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
            _logger.Fatal(exception, "AdscContractExpiryService failed");
        }

        _logger.Information("Stopped ADS-C contract expiry service");
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        _task?.Dispose();
    }
}
