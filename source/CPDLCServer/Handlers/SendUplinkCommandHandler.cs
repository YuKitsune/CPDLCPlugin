using CPDLCServer.Clients;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Services;
using MediatR;


namespace CPDLCServer.Handlers;

public class SendUplinkCommandHandler(
    IAircraftRepository aircraftRepository,
    IClientManager clientManager,
    IMessageIdProvider messageIdProvider,
    IDialogueRepository dialogueRepository,
    IMediator mediator,
    IClock clock,
    ILogger logger)
    : IRequestHandler<SendUplinkCommand, SendUplinkResult>
{
    public async Task<SendUplinkResult> Handle(SendUplinkCommand request, CancellationToken cancellationToken)
    {
        logger.Information("Sending uplink message to {Callsign}", request.Recipient);

        var allAircraft = await aircraftRepository.All(cancellationToken);
        var aircraftConnection =
            allAircraft.FirstOrDefault(a => a.Callsign == request.Recipient && a.DataAuthorityState == DataAuthorityState.CurrentDataAuthority) // Prefer the CDA connection
            ?? allAircraft.FirstOrDefault(a => a.Callsign == request.Recipient && a.DataAuthorityState == DataAuthorityState.NextDataAuthority); // Defer to NDA connection if there's no CDA connection
        if (aircraftConnection is null)
            throw new Exception($"{request.Recipient} is not connected");

        var client = await clientManager.GetAcarsClient(aircraftConnection.AcarsClientId, cancellationToken);

        var messageId = await messageIdProvider.GetNextMessageId(
            aircraftConnection.AcarsClientId,
            request.Recipient,
            cancellationToken);

        var uplinkMessage = new UplinkMessage(
            messageId,
            request.ReplyToDownlinkId,
            request.Recipient,
            request.Sender,
            request.ResponseType,
            AlertType.None,
            request.Content,
            clock.UtcNow());

        // Add or update the dialogue
        var dialogue = request.ReplyToDownlinkId.HasValue
            ? await dialogueRepository.FindDialogueForMessage(
                request.Recipient,
                request.ReplyToDownlinkId.Value,
                cancellationToken)
            : null;

        if (dialogue is null)
        {
            dialogue = new Dialogue(request.Recipient, uplinkMessage);
            await dialogueRepository.Add(dialogue, cancellationToken);
            logger.Information("Dialogue {DialogueId} created for uplink message to {Callsign}", dialogue.Id, request.Recipient);
        }
        else
        {
            dialogue.AddMessage(uplinkMessage);
            logger.Information("Uplink message to {Callsign} added to dialogue {DialogueId}", request.Recipient, dialogue.Id);
        }

        // Publish dialogue change notification
        await mediator.Publish(new DialogueChangedNotification(dialogue), cancellationToken);

        if (ControlMessages.IsEndServiceUplink(uplinkMessage))
        {
            await mediator.Send(new TerminateConnectionRequest(uplinkMessage.Recipient, aircraftConnection.AcarsClientId), cancellationToken);
        }

        await client.Send(uplinkMessage, cancellationToken);
        logger.Information(
            "Sent CPDLC message from {Sender} to {PilotCallsign}",
            request.Sender,
            uplinkMessage.Recipient);

        return new SendUplinkResult(uplinkMessage);
    }
}
