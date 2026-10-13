using CPDLCServer.Clients;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class RequestPeriodicAdscContractCommandHandler(
    IClientManager clientManager,
    IAdscContractRepository adscContractRepository,
    IMediator mediator,
    IClock clock,
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

        logger.Information(
            "Requesting periodic ADS-C contract with {Callsign} from {StationId} (Interval: {ReportingInterval})",
            request.Callsign,
            request.StationId,
            request.ReportingInterval);

        var client = await clientManager.GetAcarsClientForStation(request.StationId, cancellationToken);
        await client.RequestPeriodicAdscContract(request.Callsign, request.ReportingInterval, cancellationToken);

        // A new request replaces the existing contract on the aircraft
        var contractKey = new AdscContractKey(request.Callsign, request.StationId);
        var contract = await adscContractRepository.Find(contractKey, cancellationToken);
        if (contract is null)
        {
            contract = new AdscContract(
                request.Callsign,
                request.StationId,
                request.ReportingInterval,
                clock.UtcNow());

            await adscContractRepository.Add(contractKey, contract, cancellationToken);
        }
        else
        {
            contract.ChangeReportingInterval(request.ReportingInterval, clock.UtcNow());
        }

        await mediator.Publish(new AdscContractChangedNotification(contract), cancellationToken);
    }
}
