using CPDLCServer.AcarsConnections;
using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Infrastructure;
using MediatR;

namespace CPDLCServer.Cpdlc.Uplinks;

public class ReplyToDownlinkCommandHandler(
    IAircraftRepository aircraftRepository,
    IAcarsConnectionManager acarsConnectionManager,
    IMessageIdProvider messageIdProvider,
    IDialogueRepository dialogueRepository,
    IMediator mediator,
    IClock clock,
    IUplinkTransmitter uplinkTransmitter,
    ILogger logger)
    : IRequestHandler<ReplyToDownlinkCommand>
{
    public async Task Handle(ReplyToDownlinkCommand request, CancellationToken cancellationToken)
    {
        var dialogue = await dialogueRepository.FindById(request.DialogueId, cancellationToken);
        if (dialogue is null)
            throw new Exception($"Dialogue {request.DialogueId} not found");

        var callsign = dialogue.AircraftCallsign;
        logger.Information("Replying to downlink {DownlinkMessageId} in dialogue {DialogueId} for {Callsign}",
            request.DownlinkMessageId, request.DialogueId, callsign);

        // The reply goes through the connection of the ATSU of the dialogue
        var allAircraft = await aircraftRepository.All(cancellationToken);
        var aircraftConnection = allAircraft.FirstOrDefault(a => a.Callsign == callsign && a.StationId == dialogue.StationId);
        if (aircraftConnection is null)
            throw new Exception($"{callsign} is not connected");

        var acarsConnection = await acarsConnectionManager.GetAcarsConnection(aircraftConnection.AcarsConnectionId, cancellationToken);

        var messageId = await messageIdProvider.GetNextMessageId(
            aircraftConnection.AcarsConnectionId,
            callsign,
            cancellationToken);

        var uplinkMessage = dialogue.AddUplink(
            messageId,
            request.DownlinkMessageId,
            callsign,
            request.Sender,
            request.ResponseType,
            AlertType.None,
            request.Content,
            clock.UtcNow());

        logger.Information("Uplink {MessageId} added to dialogue {DialogueId} as reply to downlink {DownlinkMessageId}",
            messageId, request.DialogueId, request.DownlinkMessageId);

        await mediator.Publish(new DialogueChangedNotification(dialogue), cancellationToken);

        uplinkTransmitter.TransmitAfterCommit(acarsConnection, uplinkMessage);
    }
}
