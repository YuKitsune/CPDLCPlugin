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
                if (!contract.IsActive)
                {
                    logger.Information(
                        "{Callsign} rejected the ADS-C contract with {StationId}, ignoring ADS-C report",
                        message.Sender,
                        notification.StationId);
                    return;
                }

                logger.Information(
                    "ADS-C report received from {Callsign}",
                    message.Sender);

                if (contract.ReportReceived())
                {
                    logger.Information(
                        "{Callsign} accepted the ADS-C contract with {StationId}",
                        message.Sender,
                        notification.StationId);

                    await mediator.Publish(new AdscContractChangedNotification(contract), cancellationToken);
                }

                await mediator.Publish(
                    new AdscReportReceivedNotification(
                        message.Sender,
                        notification.StationId,
                        reportMessage.Report,
                        reportMessage.Received),
                    cancellationToken);
                break;

            case ReceivedAdscRejection:
                var selectedContractRejected = contract.Reject();
                logger.Information(
                    "{Callsign} rejected the ADS-C contract with {StationId}",
                    message.Sender,
                    notification.StationId);

                await mediator.Publish(new AdscContractChangedNotification(contract), cancellationToken);

                // Re-establish the default contract
                if (selectedContractRejected)
                {
                    await mediator.Send(
                        new RequestDefaultAdscContractCommand(notification.StationId, message.Sender),
                        cancellationToken);
                }
                break;

            default:
                throw new ArgumentException($"Unknown ADS-C message type: {message.GetType()}");
        }
    }
}
