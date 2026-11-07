using CPDLCServer.AcarsConnections;
using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.AdscContracts;

public class EndExpiredAdscContractsRequestHandler(
    IAcarsConnectionManager acarsConnectionManager,
    IAdscContractRepository adscContractRepository,
    IClock clock,
    ServerStartTime serverStartTime,
    IMediator mediator,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<EndExpiredAdscContractsRequest>
{
    public async Task Handle(EndExpiredAdscContractsRequest request, CancellationToken cancellationToken)
    {
        var contracts = await adscContractRepository.All(cancellationToken);
        var now = clock.UtcNow();

        // The last activity times do not change while the server is stopped
        var canApplyContractTimeout = now - serverStartTime.Started > request.ContractTimeout;

        foreach (var contract in contracts)
        {
            if (canApplyContractTimeout && now - contract.LastActivity > request.ContractTimeout)
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
        unitOfWork.AfterCommit(async ct =>
        {
            try
            {
                var acarsConnection = await acarsConnectionManager.GetAcarsConnectionForStation(contract.StationId, ct);
                await acarsConnection.CancelAdscContract(contract.Callsign, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.Warning(
                    ex,
                    "Failed to cancel ADS-C contract with {Callsign} for {StationId} on the ACARS network",
                    contract.Callsign,
                    contract.StationId);
            }
        });

        await adscContractRepository.Remove(new AdscContractKey(contract.Callsign, contract.StationId), cancellationToken);

        await mediator.Publish(
            new AdscContractRemovedNotification(contract.Callsign, contract.StationId),
            cancellationToken);
    }
}
