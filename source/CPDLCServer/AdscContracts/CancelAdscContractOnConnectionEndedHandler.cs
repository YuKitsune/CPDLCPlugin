using CPDLCServer.AcarsConnections;
using CPDLCServer.Cpdlc.Connections;
using MediatR;

namespace CPDLCServer.AdscContracts;

// VATSIM accommodation: in real operations, ADS-C does not depend on CPDLC.
// The aircraft makes ADS-C contracts after an AFN logon, and an ATSU usually keeps its contract after a CPDLC transfer,
// until the aircraft is clear of its airspace.
// Hoppie has no AFN logon, so the CPDLC connection is the only signal that an aircraft is online.
// Thus the server cancels the ADS-C contract when the CPDLC connection ends, and when a new logon resets it.
public class CancelAdscContractOnConnectionEndedHandler(
    IAcarsConnectionManager acarsConnectionManager,
    IAdscContractRepository adscContractRepository,
    IMediator mediator,
    ILogger logger)
    : INotificationHandler<AircraftConnectionTerminated>,
        INotificationHandler<AircraftConnectionLost>,
        INotificationHandler<AircraftConnectionReset>
{
    public async Task Handle(AircraftConnectionTerminated notification, CancellationToken cancellationToken)
    {
        await CancelContract(notification.Callsign, notification.StationId, cancellationToken);
    }

    public async Task Handle(AircraftConnectionLost notification, CancellationToken cancellationToken)
    {
        var acarsConnection = await acarsConnectionManager.GetAcarsConnection(notification.AcarsConnectionId, cancellationToken);
        await CancelContract(notification.Callsign, acarsConnection.StationId, cancellationToken);
    }

    // The contract was made with the previous logon.
    // Cancel it, so that the server requests a new contract for the new logon.
    public async Task Handle(AircraftConnectionReset notification, CancellationToken cancellationToken)
    {
        await CancelContract(notification.Callsign, notification.StationId, cancellationToken);
    }

    async Task CancelContract(string callsign, string stationId, CancellationToken cancellationToken)
    {
        // A failure here must not stop the other handlers for the ended CPDLC connection
        try
        {
            var contract = await adscContractRepository.Find(new AdscContractKey(callsign, stationId), cancellationToken);
            if (contract is null)
                return;

            await mediator.Send(new CancelAdscContractCommand(stationId, callsign), cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.Error(
                ex,
                "Failed to cancel ADS-C contract with {Callsign} for {StationId}",
                callsign,
                stationId);
        }
    }
}
