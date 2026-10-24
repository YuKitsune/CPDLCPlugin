using CPDLCServer.Clients;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class RequestSelectedAdscContractCommandHandler(
    IClientManager clientManager,
    IAdscContractRepository adscContractRepository,
    IMediator mediator,
    IClock clock,
    ILogger logger)
    : IRequestHandler<RequestSelectedAdscContractCommand>
{
    public async Task Handle(RequestSelectedAdscContractCommand request, CancellationToken cancellationToken)
    {
        if (request.ReportingInterval < AdscContract.MinimumReportingInterval)
            throw new ArgumentOutOfRangeException(
                nameof(request.ReportingInterval),
                request.ReportingInterval,
                $"Reporting interval must be {AdscContract.MinimumReportingInterval.TotalSeconds} seconds or more");

        logger.Information(
            "Requesting selected ADS-C contract with {Callsign} from {StationId} (Interval: {ReportingInterval})",
            request.Callsign,
            request.StationId,
            request.ReportingInterval);

        var client = await clientManager.GetAcarsClientForStation(request.StationId, cancellationToken);
        await client.RequestPeriodicAdscContract(request.Callsign, request.ReportingInterval, cancellationToken);

        // The selected contract ends at a fixed time after the request is sent
        var configuration = await clientManager.GetConfigurationForStation(request.StationId, cancellationToken);
        var ends = clock.UtcNow() + TimeSpan.FromMinutes(configuration.SelectedAdscContractDuration);

        var contractKey = new AdscContractKey(request.Callsign, request.StationId);
        var contract = await adscContractRepository.Find(contractKey, cancellationToken);
        if (contract is null)
        {
            contract = new AdscContract(request.Callsign, request.StationId);
            await adscContractRepository.Add(contractKey, contract, cancellationToken);
        }

        contract.RequestSelectedContract(request.ReportingInterval, ends);
        contract.LogActivity(clock.UtcNow());

        await mediator.Publish(new AdscContractChangedNotification(contract), cancellationToken);
    }
}
