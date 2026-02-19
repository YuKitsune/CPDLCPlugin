using CPDLCServer.Clients;
using CPDLCServer.Exceptions;
using CPDLCServer.Messages;
using MediatR;

namespace CPDLCServer.Services;

public class AircraftConnectivityCheckService : IHostedService, IDisposable
{
    readonly TimeSpan _checkInterval;
#if DEBUG
    readonly TimeSpan _errorInterval = TimeSpan.FromSeconds(5);
#else
    readonly TimeSpan _errorInterval = TimeSpan.FromMinutes(5);
#endif

    readonly AcarsConfiguration[] _acarsConfigurations;
    readonly IMediator _mediator;
    readonly ILogger _logger;

    CancellationTokenSource? _cancellationTokenSource;
    Task? _task;

    public AircraftConnectivityCheckService(IConfiguration configuration, IMediator mediator, ILogger logger)
    {
        _mediator = mediator;
        _logger = logger.ForContext<AircraftConnectivityCheckService>();

        var checkIntervalMinutes = configuration.GetValue("ConnectivityCheckIntervalMinutes", 15);
        _checkInterval = TimeSpan.FromMinutes(checkIntervalMinutes);

        var acarsConfigurationSection = configuration.GetSection("Acars");
        var configurationList = new List<AcarsConfiguration>();

        foreach (var section in acarsConfigurationSection.GetChildren())
        {
            var type = section["Type"];
            var config = type switch
            {
                "Hoppie" => section.Get<HoppiesConfiguration>(),
                _ => throw new NotSupportedException($"ACARS configuration type '{type}' is not supported")
            };

            if (config is not null)
            {
                configurationList.Add(config);
            }
        }

        _acarsConfigurations = configurationList.ToArray();
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

                    foreach (var acarsConfiguration in _acarsConfigurations)
                    {
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        timeoutCts.CancelAfter(TimeSpan.FromMinutes(1));

                        await _mediator.Send(
                            new CheckAircraftConnectionsRequest(acarsConfiguration.ClientId),
                            timeoutCts.Token);
                    }

                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ConfigurationNotFoundException)
                {
                    _logger.Debug("ACARS client not yet available, retrying");
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
            _logger.Fatal(exception, "AircraftConnectivityCheckService failed");
        }

        _logger.Information("Stopped aircraft connectivity check service");
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        _task?.Dispose();
    }
}
