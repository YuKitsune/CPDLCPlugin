using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class AdscMessageReceivedNotificationHandler(
    IAdscContractRepository adscContractRepository,
    IMediator mediator,
    ILogger logger)
    : INotificationHandler<AdscMessageReceivedNotification>
{
    public async Task Handle(AdscMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var message = notification.Message;
        var contractKey = new AdscContractKey(message.Sender, notification.StationId);

        var contract = await adscContractRepository.Find(contractKey, cancellationToken);
        if (contract is null)
        {
            logger.Information(
                "{Callsign} has no ADS-C contract with {StationId}, ignoring ADS-C message",
                message.Sender,
                notification.StationId);
            return;
        }

        switch (message)
        {
            case ReceivedAdscReport reportMessage:
                logger.Information(
                    "ADS-C report received from {Callsign}",
                    message.Sender);

                await mediator.Publish(
                    new AdscReportReceivedNotification(
                        message.Sender,
                        notification.StationId,
                        reportMessage.Report,
                        reportMessage.Received),
                    cancellationToken);
                break;

            case ReceivedAdscRejection:
                await adscContractRepository.Remove(contractKey, cancellationToken);
                logger.Information(
                    "{Callsign} rejected the ADS-C contract with {StationId}",
                    message.Sender,
                    notification.StationId);

                await mediator.Publish(
                    new AdscContractRemovedNotification(message.Sender, notification.StationId),
                    cancellationToken);
                break;

            default:
                throw new ArgumentException($"Unknown ADS-C message type: {message.GetType()}");
        }
    }
}
