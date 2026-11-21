using CPDLCServer.AcarsConnections;
using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Cpdlc.Uplinks;
using MediatR;

namespace CPDLCServer.Cpdlc.Motd;

// Sends the message of the day as a CPDLC free text message with no response.
// The message is not sent if the aircraft disconnected during the delay, or if the message was removed.
public class SendMotdCommandHandler(
    IAircraftRepository aircraftRepository,
    IAcarsConnectionManager acarsConnectionManager,
    IMediator mediator,
    ILogger logger)
    : IRequestHandler<SendMotdCommand>
{
    public async Task Handle(SendMotdCommand request, CancellationToken cancellationToken)
    {
        var aircraftConnection = await aircraftRepository.Find(
            new AircraftKey(request.Callsign, request.AcarsConnectionId),
            cancellationToken);
        if (aircraftConnection is not { DataAuthorityState: DataAuthorityState.CurrentDataAuthority })
        {
            logger.Information("Message of the day not sent to {Callsign}: the aircraft is not connected", request.Callsign);
            return;
        }

        var configuration = await acarsConnectionManager.GetConfigurationForStation(aircraftConnection.StationId, cancellationToken);
        if (string.IsNullOrWhiteSpace(configuration.Motd))
            return;

        logger.Information("Sending the message of the day to {Callsign}", request.Callsign);

        await mediator.Send(
            new BeginDialogueCommand(
                aircraftConnection.StationId,
                request.Callsign,
                CpdlcUplinkResponseType.NoResponse,
                configuration.Motd),
            cancellationToken);
    }
}
