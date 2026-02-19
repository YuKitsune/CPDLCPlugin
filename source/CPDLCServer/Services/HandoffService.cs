using CPDLCServer.Exceptions;
using CPDLCServer.Messages;
using MediatR;

namespace CPDLCServer.Services;

/// <summary>
///     Periodically dispatches <see cref="ProcessHandoffsCommand"/> to ensure handoff messages are sent to aircraft
///     within the configured lead time before their expected transfer.
/// </summary>
public class HandoffService(IMediator mediator, ILogger logger) : IHostedService
{
    readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
    readonly TimeSpan _errorInterval = TimeSpan.FromSeconds(5);

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

    async Task DoWork(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var checkTimeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                checkTimeoutCancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

                try
                {
                    await mediator.Send(new ProcessHandoffsCommand(), checkTimeoutCancellationTokenSource.Token);
                    await Task.Delay(_interval, cancellationToken);
                }
                catch (OperationCanceledException) when (checkTimeoutCancellationTokenSource.IsCancellationRequested)
                {
                    // Timeout
                    logger.Warning("HandoffService check timed out");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Stopping
                }
                catch (ConfigurationNotFoundException)
                {
                    // Race condition: Client manager probably hasn't created the client yet
                    await Task.Delay(_errorInterval, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.Error(exception, "Error sending handoff message");
                    await Task.Delay(_errorInterval, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Stopping
            logger.Information("Stopping HandoffService...");
        }
        catch (Exception exception)
        {
            logger.Fatal(exception, "Error sending handoff message");
        }
    }
}
