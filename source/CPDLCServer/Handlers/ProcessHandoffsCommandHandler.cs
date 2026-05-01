using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class ProcessHandoffsCommandHandler(
    IAircraftRepository aircraftRepository,
    IAcarsConnectedCallsignsRepository acarsConnectedCallsignsRepository,
    IMediator mediator,
    IClock clock,
    ILogger logger,
    IConfiguration configuration)
    : IRequestHandler<ProcessHandoffsCommand>
{
    readonly TimeSpan _notificationLeadTime = TimeSpan.FromMinutes(
        configuration["Handoff:NotificationLeadTime"] is { } value && int.TryParse(value, out var minutes) ? minutes : 20);

    public async Task Handle(ProcessHandoffsCommand request, CancellationToken cancellationToken)
    {
        var trackedConnections = await aircraftRepository.All(cancellationToken);

        // Fetch the cached list of ACARS-connected callsigns once per cycle to avoid
        // repeated repository calls for each aircraft.
        var onlineCallsigns = await acarsConnectedCallsignsRepository.All(cancellationToken);

        foreach (var aircraftConnection in trackedConnections)
        {
            // TODO Test Case: When aircraft has no NextDataAuthority, no messages are sent
            if (!aircraftConnection.HasNextDataAuthority)
                continue; // No NDA, nothing to do

            // TODO Test Case: When a handoff message has already been sent, no new messages are sent
            if (aircraftConnection.DidSentNextDataAuthorityMessage)
                continue; // Already sent, nothing to do

            // TODO Test Case: When NextDataAuthority is set, and we're not within the specified time window, no messages are sent
            var transmitTime = aircraftConnection.ExpectedTransferTime.Value.Subtract(_notificationLeadTime);
            if (clock.UtcNow() <= transmitTime)
                continue; // Too early, nothing to do

            // Only transmit if the NDA ATSU is currently reachable on the ACARS network.
            // If it's offline, skip for now and retry on the next cycle.
            if (!onlineCallsigns.Contains(aircraftConnection.NextDataAuthority!, StringComparer.OrdinalIgnoreCase))
            {
                logger.Warning(
                    "NDA ATSU {NextDataAuthority} is not online, skipping handoff message for {Callsign}",
                    aircraftConnection.NextDataAuthority,
                    aircraftConnection.Callsign);
                continue;
            }

            logger.Information("Sending handoff {NextDataAuthority} message to {Callsign}", aircraftConnection.NextDataAuthority, aircraftConnection.Callsign);

            if (clock.UtcNow() > aircraftConnection.ExpectedTransferTime)
            {
                logger.Information("Handoff message for {Callsign} is being transmitted after the expected transfer time {TransferTime}",  aircraftConnection.Callsign, aircraftConnection.ExpectedTransferTime);
            }

            // TODO Test Case: When NextDataAuthority is set, and we're within the specified time window, NEXT DATA AUTHORITY message is sent
            await mediator.Send(
                new SendUplinkCommand(
                    aircraftConnection.StationId,
                    aircraftConnection.Callsign,
                    null,
                    CpdlcUplinkResponseType.NoResponse,
                    $"NEXT DATA AUTHORITY @{aircraftConnection.NextDataAuthority}@"),
                cancellationToken);

            // TODO Test Case: When NextDataAuthority message is sent connection is updated
            aircraftConnection.SentNextDataAuthorityMessage();
        }
    }
}
