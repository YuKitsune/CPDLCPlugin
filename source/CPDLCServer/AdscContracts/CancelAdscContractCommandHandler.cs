using CPDLCServer.AcarsConnections;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.AdscContracts;

public class CancelAdscContractCommandHandler(
    IAcarsConnectionManager acarsConnectionManager,
    IAdscContractRepository adscContractRepository,
    IMediator mediator,
    IUnitOfWork unitOfWork,
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

        var acarsConnection = await acarsConnectionManager.GetAcarsConnectionForStation(request.StationId, cancellationToken);
        unitOfWork.AfterCommit(ct => acarsConnection.CancelAdscContract(request.Callsign, ct));

        await adscContractRepository.Remove(contractKey, cancellationToken);

        await mediator.Publish(
            new AdscContractRemovedNotification(request.Callsign, request.StationId),
            cancellationToken);
    }
}
