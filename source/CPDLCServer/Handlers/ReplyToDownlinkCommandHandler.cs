using CPDLCServer.Clients;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Services;
using MediatR;

namespace CPDLCServer.Handlers;

public class ReplyToDownlinkCommandHandler(
    IAircraftRepository aircraftRepository,
    IClientManager clientManager,
    IMessageIdProvider messageIdProvider,
    IDialogueRepository dialogueRepository,
    IMediator mediator,
    IClock clock,
    ILogger logger)
    : IRequestHandler<ReplyToDownlinkCommand, SendUplinkResult>
{
    public async Task<SendUplinkResult> Handle(ReplyToDownlinkCommand request, CancellationToken cancellationToken)
    {
        var dialogue = await dialogueRepository.FindById(request.DialogueId, cancellationToken);
        if (dialogue is null)
            throw new Exception($"Dialogue {request.DialogueId} not found");

        var callsign = dialogue.AircraftCallsign;
        logger.Information("Replying to downlink {DownlinkMessageId} in dialogue {DialogueId} for {Callsign}",
            request.DownlinkMessageId, request.DialogueId, callsign);

        var allAircraft = await aircraftRepository.All(cancellationToken);
        var aircraftConnection =
            allAircraft.FirstOrDefault(a => a.Callsign == callsign && a.DataAuthorityState == DataAuthorityState.CurrentDataAuthority)
            ?? allAircraft.FirstOrDefault(a => a.Callsign == callsign && a.DataAuthorityState == DataAuthorityState.NextDataAuthority);
        if (aircraftConnection is null)
            throw new Exception($"{callsign} is not connected");

        var client = await clientManager.GetAcarsClient(aircraftConnection.AcarsClientId, cancellationToken);

        var messageId = await messageIdProvider.GetNextMessageId(
            aircraftConnection.AcarsClientId,
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

        await client.Send(uplinkMessage, cancellationToken);
        logger.Information("Sent CPDLC message from {Sender} to {Callsign}", request.Sender, callsign);

        return new SendUplinkResult(uplinkMessage);
    }
}
