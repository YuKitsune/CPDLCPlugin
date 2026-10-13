using CPDLCServer.Clients;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class RequestAdscDemandReportCommandHandler(
    IClientManager clientManager,
    IAdscContractRepository adscContractRepository,
    IMediator mediator,
    IClock clock,
    ILogger logger)
    : IRequestHandler<RequestAdscDemandReportCommand>
{
    public async Task Handle(RequestAdscDemandReportCommand request, CancellationToken cancellationToken)
    {
        logger.Information(
            "Requesting ADS-C demand report from {Callsign} for {StationId}",
            request.Callsign,
            request.StationId);

        var client = await clientManager.GetAcarsClientForStation(request.StationId, cancellationToken);
        await client.RequestAdscDemandReport(request.Callsign, cancellationToken);

        // A demand report does not change an existing periodic contract.
        // Without a contract, the report would be ignored, so add a contract with no reporting interval.
        var contractKey = new AdscContractKey(request.Callsign, request.StationId);
        var contract = await adscContractRepository.Find(contractKey, cancellationToken);
        if (contract is not null)
            return;

        contract = new AdscContract(
            request.Callsign,
            request.StationId,
            null,
            clock.UtcNow());

        await adscContractRepository.Add(contractKey, contract, cancellationToken);

        await mediator.Publish(new AdscContractChangedNotification(contract), cancellationToken);
    }
}
