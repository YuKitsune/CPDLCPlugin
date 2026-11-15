using CPDLCServer.Infrastructure;
using MediatR;

namespace CPDLCServer.Cpdlc.Dialogues;

public class MessageMonitorService(
    IUnitOfWorkMediator mediator,
    IClock clock,
    ILogger logger)
    : BackgroundService
{
    readonly TimeSpan _timeoutCheckInterval = TimeSpan.FromSeconds(5);

    // The retention period is in hours, so the purge does not need to run often
    readonly TimeSpan _purgeInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.Information("Starting message monitor service");
        DateTimeOffset? lastPurge = null;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.Verbose("Running message monitor iteration");

                // Each request is a separate unit of work, so that a failure in one does not stop the others
                var now = clock.UtcNow();
                if (lastPurge is null || now - lastPurge >= _purgeInterval)
                {
                    await Send(new PurgeExpiredCpdlcDataRequest(), stoppingToken);
                    lastPurge = now;
                }

                await Send(new CheckMessageTimeoutsRequest(), stoppingToken);
                await Send(new ArchiveCompletedDialoguesRequest(), stoppingToken);

                await Task.Delay(_timeoutCheckInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.Information("Message monitor task stopped");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Fatal error in message monitor task");
        }
    }

    async Task Send(IRequest request, CancellationToken stoppingToken)
    {
        try
        {
            await mediator.Send(request, stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            logger.Error(ex, "Error during message monitoring ({RequestType})", request.GetType().Name);
        }
    }
}
