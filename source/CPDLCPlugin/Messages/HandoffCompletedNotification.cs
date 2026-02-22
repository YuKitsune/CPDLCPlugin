using CPDLCServer.Contracts;
using MediatR;
using Serilog;
using vatsys;

namespace CPDLCPlugin.Messages;

public record HandoffCompletedNotification(string Callsign, string? NextControllerCallsign) : INotification;

public class HandoffCompletedNotificationHandler(
    Plugin plugin,
    AircraftConnectionStore aircraftConnectionStore,
    ControllerConnectionStore controllerConnectionStore,
    AtisCache atisCache,
    IMediator mediator,
    ILogger logger)
    : INotificationHandler<HandoffCompletedNotification>
{
    public async Task Handle(HandoffCompletedNotification notification, CancellationToken cancellationToken)
    {
        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
            return;

        var aircraftConnections = await aircraftConnectionStore.All(cancellationToken);
        var aircraftConnection = aircraftConnections.FirstOrDefault(c =>
            c.Callsign == notification.Callsign &&
            c.DataAuthorityState == DataAuthorityState.CurrentDataAuthority);
        if (aircraftConnection is null)
            return;

        if (string.IsNullOrEmpty(notification.NextControllerCallsign))
        {
            // Handoff to none, logoff
            logger.Information("{Callsign} handoff to NONE, sending END SERVICE uplink", aircraftConnection.Callsign, notification.NextControllerCallsign);
            await SendLogoffUplink(notification.Callsign, cancellationToken);
            return;
        }

        var fdr = FDP2.GetFDRs.FirstOrDefault(f => f.Callsign == notification.Callsign);
        if (fdr is null)
        {
            // FDR is missing???
            logger.Information("{Callsign} FDR not found, sending END SERVICE uplink", aircraftConnection.Callsign);
            await SendLogoffUplink(notification.Callsign, cancellationToken);
            return;
        }

        // Check if next controller is using CPDLC
        var nextStationCode = await LogonCodeHelper.TryGetLogonCode(
            notification.NextControllerCallsign!, // Safe to unwrap, as we've checked above
            controllerConnectionStore,
            atisCache,
            cancellationToken);
        if (!string.IsNullOrEmpty(nextStationCode))
        {
            if (plugin.ConnectionManager.StationIdentifier != nextStationCode)
            {
                // Next controller has CPDLC code for different ATSU
                // Send END SERVICE so aircraft disconnects and reconnects to next ATSU
                logger.Information(
                    "{Callsign} handed off to {NextControllerCallsign} with CPDLC code {StationCode} (different ATSU), sending END SERVICE uplink",
                    aircraftConnection.Callsign,
                    notification.NextControllerCallsign,
                    nextStationCode);
                await SendLogoffUplink(notification.Callsign, cancellationToken);
                return;
            }

            // Next controller has same CPDLC code as us
            logger.Information(
                "{Callsign} handed off to {NextControllerCallsign} within same ATSU (code {StationCode})",
                aircraftConnection.Callsign,
                notification.NextControllerCallsign,
                nextStationCode);
            return;
        }

        // Next controller does not have CPDLC, send END SERVICE
        logger.Information(
            "{Callsign} handed off to {NextControllerCallsign} without CPDLC, sending END SERVICE",
            aircraftConnection.Callsign,
            notification.NextControllerCallsign);
        await SendLogoffUplink(notification.Callsign, cancellationToken);
    }

    async Task SendLogoffUplink(string recipient, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new SendUplinkRequest(
                recipient,
                null,
                CpdlcUplinkResponseType.NoResponse,
                "END SERVICE"),
            cancellationToken);
    }
}
