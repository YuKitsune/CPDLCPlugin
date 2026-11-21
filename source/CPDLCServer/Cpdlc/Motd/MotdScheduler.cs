using CPDLCServer.Infrastructure;

namespace CPDLCServer.Cpdlc.Motd;

/// <summary>
/// Sends the message of the day of an ACARS connection to an aircraft after a delay.
/// The delay runs in the background, so that the logon does not wait for it.
/// </summary>
public class MotdScheduler(
    IUnitOfWorkMediator mediator,
    IHostApplicationLifetime lifetime,
    ILogger logger)
{
    public void Schedule(string acarsConnectionId, string callsign, TimeSpan delay)
    {
        _ = Task.Run(async () =>
        {
            var cancellationToken = lifetime.ApplicationStopping;

            try
            {
                await Task.Delay(delay, cancellationToken);
                await mediator.Send(new SendMotdCommand(acarsConnectionId, callsign), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The server is stopping
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to send the message of the day to {Callsign} on {AcarsConnectionId}", callsign, acarsConnectionId);
            }
        });
    }
}
