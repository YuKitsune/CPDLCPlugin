using CPDLCServer.Clients;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class RequestDefaultAdscContractCommandHandler(
    IClientManager clientManager,
    IAdscContractRepository adscContractRepository,
    IMediator mediator,
    ILogger logger)
    : IRequestHandler<RequestDefaultAdscContractCommand>
{
    public async Task Handle(RequestDefaultAdscContractCommand request, CancellationToken cancellationToken)
    {
        var configuration = await clientManager.GetConfigurationForStation(request.StationId, cancellationToken);
        var reportingInterval = TimeSpan.FromMinutes(configuration.DefaultAdscReportingInterval);

        logger.Information(
            "Requesting default ADS-C contract with {Callsign} from {StationId} (Interval: {ReportingInterval})",
            request.Callsign,
            request.StationId,
            reportingInterval);

        var client = await clientManager.GetAcarsClientForStation(request.StationId, cancellationToken);
        await client.RequestPeriodicAdscContract(request.Callsign, reportingInterval, cancellationToken);

        var contractKey = new AdscContractKey(request.Callsign, request.StationId);
        var contract = await adscContractRepository.Find(contractKey, cancellationToken);
        if (contract is null)
        {
            contract = new AdscContract(request.Callsign, request.StationId);
            await adscContractRepository.Add(contractKey, contract, cancellationToken);
        }

        contract.RequestDefaultContract(reportingInterval);

        await mediator.Publish(new AdscContractChangedNotification(contract), cancellationToken);
    }
}
