using CPDLCServer.Clients;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Services;
using MediatR;

namespace CPDLCServer.Handlers;

public class BeginDialogueCommandHandler(
    IAircraftRepository aircraftRepository,
    IClientManager clientManager,
    IMessageIdProvider messageIdProvider,
    IDialogueRepository dialogueRepository,
    IMediator mediator,
    IClock clock,
    ILogger logger)
    : IRequestHandler<BeginDialogueCommand, SendUplinkResult>
{
    public async Task<SendUplinkResult> Handle(BeginDialogueCommand request, CancellationToken cancellationToken)
    {
        logger.Information("Beginning new dialogue with {Recipient} (Content: {Content})",
            request.Recipient, request.Content);

        var allAircraft = await aircraftRepository.All(cancellationToken);
        var aircraftConnection =
            allAircraft.FirstOrDefault(a => a.Callsign == request.Recipient && a.DataAuthorityState == DataAuthorityState.CurrentDataAuthority)
            ?? allAircraft.FirstOrDefault(a => a.Callsign == request.Recipient && a.DataAuthorityState == DataAuthorityState.NextDataAuthority);
        if (aircraftConnection is null)
            throw new Exception($"{request.Recipient} is not connected");

        var client = await clientManager.GetAcarsClient(aircraftConnection.AcarsClientId, cancellationToken);

        var messageId = await messageIdProvider.GetNextMessageId(
            aircraftConnection.AcarsClientId,
            request.Recipient,
            cancellationToken);

        var uplinkMessage = new UplinkMessage(
            messageId,
            null,
            request.Recipient,
            request.Sender,
            request.ResponseType,
            AlertType.None,
            request.Content,
            clock.UtcNow());

        var dialogue = new Dialogue(request.Recipient, uplinkMessage);
        await dialogueRepository.Add(dialogue, cancellationToken);
        logger.Information("Dialogue {DialogueId} created for uplink {MessageId} to {Callsign}",
            dialogue.Id, messageId, request.Recipient);

        await mediator.Publish(new DialogueChangedNotification(dialogue), cancellationToken);

        await client.Send(uplinkMessage, cancellationToken);
        logger.Information("Sent CPDLC message from {Sender} to {Recipient}", request.Sender, request.Recipient);

        return new SendUplinkResult(uplinkMessage);
    }
}
