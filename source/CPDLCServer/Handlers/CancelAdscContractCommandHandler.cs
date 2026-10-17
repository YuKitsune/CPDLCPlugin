using CPDLCServer.Clients;
using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class CancelAdscContractCommandHandler(
    IClientManager clientManager,
    IAdscContractRepository adscContractRepository,
    IMediator mediator,
    ILogger logger)
    : IRequestHandler<CancelAdscContractCommand>
{
    public async Task Handle(CancelAdscContractCommand request, CancellationToken cancellationToken)
    {
        var contractKey = new AdscContractKey(request.Callsign, request.StationId);
        var contract = await adscContractRepository.Find(contractKey, cancellationToken);
        if (contract is null)
        {
            logger.Warning(
                "Cannot cancel ADS-C contract with {Callsign} from {StationId}: not found",
                request.Callsign,
                request.StationId);
            return;
        }

        logger.Information(
            "Cancelling ADS-C contract with {Callsign} from {StationId}",
            request.Callsign,
            request.StationId);

        var client = await clientManager.GetAcarsClientForStation(request.StationId, cancellationToken);
        await client.CancelAdscContract(request.Callsign, cancellationToken);

        await adscContractRepository.Remove(contractKey, cancellationToken);

        await mediator.Publish(
            new AdscContractRemovedNotification(request.Callsign, request.StationId),
            cancellationToken);
    }
}
