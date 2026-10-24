using CPDLCServer.Clients;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class EndExpiredAdscContractsRequestHandler(
    IClientManager clientManager,
    IAdscContractRepository adscContractRepository,
    IClock clock,
    IMediator mediator,
    ILogger logger)
    : IRequestHandler<EndExpiredAdscContractsRequest>
{
    public async Task Handle(EndExpiredAdscContractsRequest request, CancellationToken cancellationToken)
    {
        var contracts = await adscContractRepository.All(cancellationToken);
        var now = clock.UtcNow();

        foreach (var contract in contracts)
        {
            if (now - contract.LastActivity > request.ContractTimeout)
            {
                // A failure for one aircraft must not stop the other aircraft
                try
                {
                    await CancelInactiveContract(contract, cancellationToken);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    logger.Error(
                        ex,
                        "Failed to remove inactive ADS-C contract with {Callsign} for {StationId}",
                        contract.Callsign,
                        contract.StationId);
                }

                continue;
            }

            if (contract.SelectedContractEnds is null || contract.SelectedContractEnds > now)
                continue;

            logger.Information(
                "Selected ADS-C contract with {Callsign} from {StationId} has ended",
                contract.Callsign,
                contract.StationId);

            // A failure for one aircraft must not stop the other aircraft
            try
            {
                await mediator.Send(
                    new RequestDefaultAdscContractCommand(contract.StationId, contract.Callsign),
                    cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.Error(
                    ex,
                    "Failed to request default ADS-C contract with {Callsign} for {StationId}",
                    contract.Callsign,
                    contract.StationId);
            }
        }
    }

    async Task CancelInactiveContract(AdscContract contract, CancellationToken cancellationToken)
    {
        logger.Information(
            "ADS-C contract with {Callsign} from {StationId} has no activity since {LastActivity}, cancelling",
            contract.Callsign,
            contract.StationId,
            contract.LastActivity);

        // The aircraft is possibly not on the ACARS network any more.
        // Remove the contract, even if the ACARS network cannot cancel it.
        try
        {
            var client = await clientManager.GetAcarsClientForStation(contract.StationId, cancellationToken);
            await client.CancelAdscContract(contract.Callsign, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.Warning(
                ex,
                "Failed to cancel ADS-C contract with {Callsign} for {StationId} on the ACARS network",
                contract.Callsign,
                contract.StationId);
        }

        await adscContractRepository.Remove(new AdscContractKey(contract.Callsign, contract.StationId), cancellationToken);

        await mediator.Publish(
            new AdscContractRemovedNotification(contract.Callsign, contract.StationId),
            cancellationToken);
    }
}
