using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class UpdateNextDataAuthorityCommandHandler(
    IAircraftRepository aircraftRepository,
    ILogger logger)
    : IRequestHandler<UpdateNextDataAuthorityCommand>
{
    public async Task Handle(UpdateNextDataAuthorityCommand request, CancellationToken cancellationToken)
    {
        logger.Information(
            "Updating next data authority for {Callsign} to {NextDataAuthority}",
            request.Callsign,
            request.NextDataAuthority ?? "NONE");

        var allAircraft = await aircraftRepository.All(cancellationToken);
        var aircraftConnection = allAircraft.FirstOrDefault(
            a => a.Callsign == request.Callsign && a.StationId == request.StationId);

        if (aircraftConnection is null)
        {
            logger.Warning(
                "Cannot update NDA for {Callsign} at {StationId}: not found",
                request.Callsign,
                request.StationId);
            return;
        }

        if (request.NextDataAuthority is null)
        {
            aircraftConnection.ClearNextDataAuthority();
            logger.Information("Cleared next data authority for {Callsign}", request.Callsign);
        }
        else
        {
            aircraftConnection.SetNextDataAuthority(
                request.NextDataAuthority,
                request.ExpectedTransferTime!.Value);
            logger.Information(
                "Set next data authority for {Callsign} to {NextDataAuthority} with transfer time {TransferTime}",
                request.Callsign,
                request.NextDataAuthority,
                request.ExpectedTransferTime);
        }
    }
}
