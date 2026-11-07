using CPDLCServer.AcarsConnections;
using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.AdscContracts.Reports;

public class RequestAdscDemandReportCommandHandler(
    IAcarsConnectionManager acarsConnectionManager,
    IAdscContractRepository adscContractRepository,
    IMediator mediator,
    IClock clock,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<RequestAdscDemandReportCommand>
{
    public async Task Handle(RequestAdscDemandReportCommand request, CancellationToken cancellationToken)
    {
        logger.Information(
            "Requesting ADS-C demand report from {Callsign} for {StationId}",
            request.Callsign,
            request.StationId);

        var acarsConnection = await acarsConnectionManager.GetAcarsConnectionForStation(request.StationId, cancellationToken);
        unitOfWork.AfterCommit(ct => acarsConnection.RequestAdscDemandReport(request.Callsign, ct));

        // A demand report does not change the periodic contract.
        // Without a contract, the report would be ignored, so add a contract with only the demand request.
        var contractKey = new AdscContractKey(request.Callsign, request.StationId);
        var contract = await adscContractRepository.Find(contractKey, cancellationToken);
        if (contract is null)
        {
            contract = new AdscContract(request.Callsign, request.StationId);
            await adscContractRepository.Add(contractKey, contract, cancellationToken);
        }

        contract.RequestDemandReport();
        contract.LogActivity(clock.UtcNow());

        await mediator.Publish(new AdscContractChangedNotification(contract), cancellationToken);
    }
}
