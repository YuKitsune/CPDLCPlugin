using CPDLCServer.Contracts;
using MediatR;
using Serilog;
using vatsys;

namespace CPDLCPlugin.Messages;

public record HandoffCompletedNotification(string Callsign, string? NextControllerCallsign) : INotification;

public class TrackingControllerChangedNotificationHandler(
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
        var aircraftConnection = aircraftConnections.FirstOrDefault(c => c.Callsign == notification.Callsign && c.DataAuthorityState == DataAuthorityState.CurrentDataAuthority);
        if (aircraftConnection is null)
            return;

        if (string.IsNullOrEmpty(notification.NextControllerCallsign))
        {
            // Handoff to none, logoff
            logger.Information("{Callsign} handoff accepted by {NextControllerCallsign}, sending END SERVICE uplink", aircraftConnection.Callsign, notification.NextControllerCallsign);
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

        var nextControllerConnection = await controllerConnectionStore.Find(notification.NextControllerCallsign, cancellationToken);
        if (nextControllerConnection is not null &&
            plugin.ConnectionManager.StationIdentifier != nextControllerConnection.StationId)
        {
            // Next controller is on the same server under a different station, send NDA
            logger.Information("{Callsign} handoff accepted by {NextControllerCallsign} who is connected to the CPDLC server, sending NDA to {NextDataAuthority}", aircraftConnection.Callsign, notification.Callsign, nextControllerConnection.StationId);
            await SendNextDataAuthorityUplink(notification.Callsign, nextControllerConnection.StationId, cancellationToken);
            await SendLogoffUplink(notification.Callsign, cancellationToken);
            return;
        }

        var stationCodeFromAtis = await TryGetNextStationCode(notification.Callsign, cancellationToken);
        if (!string.IsNullOrEmpty(stationCodeFromAtis) &&
            plugin.ConnectionManager.StationIdentifier != stationCodeFromAtis)
        {
            // Next station has a CPDLC code in their ATIS, and it's not the same as ours, send NDA
            logger.Information("{Callsign} handoff accepted by {NextControllerCallsign} with CPDLC code in ATIS, sending NDA to {NextDataAuthority}", aircraftConnection.Callsign, notification.Callsign, stationCodeFromAtis);
            await SendNextDataAuthorityUplink(notification.Callsign, stationCodeFromAtis, cancellationToken);
            await SendLogoffUplink(notification.Callsign, cancellationToken);
            return;
        }

        // Same data authority, nothing to do
        logger.Verbose("{Callsign} handoff accepted by {NextControllerCallsign} with same data authority, handoff not required", aircraftConnection.Callsign, notification.Callsign);
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

    async Task SendNextDataAuthorityUplink(
        string recipient,
        string nextDataAuthority,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new SendUplinkRequest(
                recipient,
                null,
                CpdlcUplinkResponseType.NoResponse,
                $"NEXT DATA AUTHORITY @{nextDataAuthority}@"),
            cancellationToken);
    }

    async Task<string> TryGetNextStationCode(string nextControllerCallsign, CancellationToken cancellationToken)
    {
        var atisLines = await atisCache.GetAtis(nextControllerCallsign, cancellationToken);

        const string cpdlcPrefix = "CPDLC";
        foreach (var atisLine in atisLines)
        {
            var cpdlcIndex = atisLine.IndexOf(cpdlcPrefix, StringComparison.OrdinalIgnoreCase);
            if (cpdlcIndex < 0)
                continue;

            var afterCpdlc = atisLine.Substring(cpdlcIndex + cpdlcPrefix.Length);
            var tokens = afterCpdlc.Split([' ', '\t', ':', '='], StringSplitOptions.RemoveEmptyEntries);

            string[] fillerWords = ["available", "on", "at"];
            foreach (var token in tokens)
            {
                if (fillerWords.Any(f => f.Equals(token, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (token.Length == 4 && token.All(char.IsLetter))
                {
                    return token.ToUpperInvariant();
                }

                break;
            }
        }

        return string.Empty;
    }
}
