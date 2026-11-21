using CPDLCServer.AcarsConnections;
using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.AdscContracts;

public class RequestPeriodicAdscContractCommandHandler(
    IAcarsConnectionManager acarsConnectionManager,
    IAdscContractRepository adscContractRepository,
    IMediator mediator,
    IClock clock,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<RequestPeriodicAdscContractCommand>
{
    public async Task Handle(RequestPeriodicAdscContractCommand request, CancellationToken cancellationToken)
    {
        if (request.ReportingInterval < AdscContract.MinimumReportingInterval)
            throw new ArgumentOutOfRangeException(
                nameof(request.ReportingInterval),
                request.ReportingInterval,
                $"Reporting interval must be {AdscContract.MinimumReportingInterval.TotalSeconds} seconds or more");

        var configuration = await acarsConnectionManager.GetConfigurationForStation(request.StationId, cancellationToken);
        var defaultReportingInterval = TimeSpan.FromMinutes(configuration.DefaultAdscReportingInterval);
        var isDefaultContract = request.ReportingInterval == defaultReportingInterval;

        logger.Information(
            "Requesting {ContractType} ADS-C contract with {Callsign} from {StationId} (Interval: {ReportingInterval})",
            isDefaultContract ? "default" : "selected",
            request.Callsign,
            request.StationId,
            request.ReportingInterval);

        var acarsConnection = await acarsConnectionManager.GetAcarsConnectionForStation(request.StationId, cancellationToken);
        unitOfWork.AfterCommit(ct => acarsConnection.RequestPeriodicAdscContract(request.Callsign, request.ReportingInterval, ct));

        var contractKey = new AdscContractKey(request.Callsign, request.StationId);
        var contract = await adscContractRepository.Find(contractKey, cancellationToken);
        var now = clock.UtcNow();
        if (contract is null)
        {
            contract = new AdscContract(request.Callsign, request.StationId, defaultReportingInterval, now);
            await adscContractRepository.Add(contractKey, contract, cancellationToken);
        }

        if (isDefaultContract)
        {
            contract.RequestDefaultContract(request.ReportingInterval);
        }
        else
        {
            contract.RequestSelectedContract(request.ReportingInterval);
        }

        contract.LogActivity(now);

        await mediator.Publish(new AdscContractChangedNotification(contract), cancellationToken);
    }
}
