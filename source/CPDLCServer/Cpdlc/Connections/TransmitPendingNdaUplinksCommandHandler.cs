using CPDLCServer.AcarsConnections.Stations;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Infrastructure;
using CPDLCServer.Settings;
using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public class TransmitPendingNdaUplinksCommandHandler(
    IAircraftRepository aircraftRepository,
    IAcarsStationRepository acarsConnectedCallsignsRepository,
    IMediator mediator,
    IClock clock,
    ILogger logger,
    IServerSettingsProvider settingsProvider)
    : IRequestHandler<TransmitPendingNdaUplinksCommand>
{
    public async Task Handle(TransmitPendingNdaUplinksCommand request, CancellationToken cancellationToken)
    {
        var settings = await settingsProvider.Load(cancellationToken);
        var notificationLeadTime = TimeSpan.FromMinutes(settings.HandoffNotificationLeadTimeMinutes);

        var trackedConnections = await aircraftRepository.All(cancellationToken);
        var onlineCallsigns = await acarsConnectedCallsignsRepository.All(cancellationToken);

        foreach (var aircraftConnection in trackedConnections)
        {
            if (!aircraftConnection.HasNextDataAuthority)
            {
                logger.Debug("{Callsign}: no NDA set, skipping", aircraftConnection.Callsign);
                continue;
            }

            if (aircraftConnection.DidSentNextDataAuthorityMessage)
            {
                logger.Debug("{Callsign}: NDA uplink already sent, skipping", aircraftConnection.Callsign);
                continue;
            }

            var transmitTime = aircraftConnection.ExpectedTransferTime.Value.Subtract(notificationLeadTime);
            if (clock.UtcNow() <= transmitTime)
            {
                logger.Debug("{Callsign}: transmit time {TransmitTime} not reached, skipping", aircraftConnection.Callsign, transmitTime);
                continue;
            }

            if (!onlineCallsigns.Contains(aircraftConnection.NextDataAuthority!, StringComparer.OrdinalIgnoreCase))
            {
                logger.Debug(
                    "{Callsign}: NDA ATSU {NextDataAuthority} is not online, skipping",
                    aircraftConnection.Callsign,
                    aircraftConnection.NextDataAuthority);
                continue;
            }

            logger.Information("Sending handoff {NextDataAuthority} message to {Callsign}", aircraftConnection.NextDataAuthority, aircraftConnection.Callsign);

            if (clock.UtcNow() > aircraftConnection.ExpectedTransferTime)
            {
                logger.Warning("Handoff message for {Callsign} is being transmitted after the expected transfer time {TransferTime}",  aircraftConnection.Callsign, aircraftConnection.ExpectedTransferTime);
            }

            await mediator.Send(
                new BeginDialogueCommand(
                    aircraftConnection.StationId,
                    aircraftConnection.Callsign,
                    CpdlcUplinkResponseType.NoResponse,
                    $"NEXT DATA AUTHORITY @{aircraftConnection.NextDataAuthority}@"),
                cancellationToken);

            aircraftConnection.SentNextDataAuthorityMessage();
        }
    }
}
