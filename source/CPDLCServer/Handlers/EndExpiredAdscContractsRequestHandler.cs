using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class EndExpiredAdscContractsRequestHandler(
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
}
