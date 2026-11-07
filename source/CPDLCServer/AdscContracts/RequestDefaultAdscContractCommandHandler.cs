using CPDLCServer.AcarsConnections;
using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.AdscContracts;

public class RequestDefaultAdscContractCommandHandler(
    IAcarsConnectionManager acarsConnectionManager,
    IAdscContractRepository adscContractRepository,
    IMediator mediator,
    IClock clock,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<RequestDefaultAdscContractCommand>
{
    public async Task Handle(RequestDefaultAdscContractCommand request, CancellationToken cancellationToken)
    {
        var configuration = await acarsConnectionManager.GetConfigurationForStation(request.StationId, cancellationToken);
        var reportingInterval = TimeSpan.FromMinutes(configuration.DefaultAdscReportingInterval);

        logger.Information(
            "Requesting default ADS-C contract with {Callsign} from {StationId} (Interval: {ReportingInterval})",
            request.Callsign,
            request.StationId,
            reportingInterval);

        var acarsConnection = await acarsConnectionManager.GetAcarsConnectionForStation(request.StationId, cancellationToken);
        unitOfWork.AfterCommit(ct => acarsConnection.RequestPeriodicAdscContract(request.Callsign, reportingInterval, ct));

        var contractKey = new AdscContractKey(request.Callsign, request.StationId);
        var contract = await adscContractRepository.Find(contractKey, cancellationToken);
        if (contract is null)
        {
            contract = new AdscContract(request.Callsign, request.StationId);
            await adscContractRepository.Add(contractKey, contract, cancellationToken);
        }

        contract.RequestDefaultContract(reportingInterval);
        contract.LogActivity(clock.UtcNow());

        await mediator.Publish(new AdscContractChangedNotification(contract), cancellationToken);
    }
}
