using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class CancelAdscContractsOnAllControllersDisconnectedHandler(
    IControllerRepository controllerRepository,
    IAdscContractRepository adscContractRepository,
    IMediator mediator,
    ILogger logger)
    : INotificationHandler<ControllerDisconnectedNotification>
{
    public async Task Handle(ControllerDisconnectedNotification notification, CancellationToken cancellationToken)
    {
        // Note: The disconnected controller is already removed from the repository
        var controllers = await controllerRepository.All(cancellationToken);
        if (controllers.Length > 0)
            return;

        var contracts = await adscContractRepository.All(cancellationToken);
        if (contracts.Length == 0)
            return;

        logger.Information(
            "No controllers are connected. Cancelling {ContractCount} ADS-C contract(s)",
            contracts.Length);

        foreach (var contract in contracts)
        {
            // A failure for one aircraft must not stop the other aircraft
            try
            {
                await mediator.Send(
                    new CancelAdscContractCommand(contract.StationId, contract.Callsign),
                    cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.Error(
                    ex,
                    "Failed to cancel ADS-C contract with {Callsign} for {StationId}",
                    contract.Callsign,
                    contract.StationId);
            }
        }
    }
}
