using CPDLCServer.Messages;
using MediatR;

namespace CPDLCServer.Services;

public class AdscContractExpiryService : IHostedService, IDisposable
{
    readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    readonly TimeSpan _contractTimeout;

    readonly IMediator _mediator;
    readonly ILogger _logger;

    CancellationTokenSource? _cancellationTokenSource;
    Task? _task;

    public AdscContractExpiryService(IConfiguration configuration, IMediator mediator, ILogger logger)
    {
        _mediator = mediator;
        _logger = logger.ForContext<AdscContractExpiryService>();

        _contractTimeout = configuration.GetValue("AdscContractTimeout", TimeSpan.FromMinutes(120));

        // The connectivity check updates the activity of each ADS-C contract at this interval.
        // If one refresh fails, the time between two updates is two refresh intervals.
        var refreshInterval = TimeSpan.FromMinutes(configuration.GetValue("ConnectivityCheckIntervalMinutes", 15));
        if (_contractTimeout <= refreshInterval * 2)
        {
            _logger.Warning(
                "AdscContractTimeout ({ContractTimeout}) is not more than two times the connectivity check interval ({RefreshInterval}). One failed refresh will cause ADS-C contracts to be cancelled.",
                _contractTimeout,
                refreshInterval);
        }
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
        _logger.Information("Starting ADS-C contract expiry service");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.Debug("Checking for ended ADS-C contracts");

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    timeoutCts.CancelAfter(TimeSpan.FromMinutes(1));

                    await _mediator.Send(new EndExpiredAdscContractsRequest(_contractTimeout), timeoutCts.Token);

                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error checking for ended ADS-C contracts");
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
