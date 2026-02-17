using CPDLCServer.Contracts;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class GetConnectedAircraftRequestHandler(IAircraftRepository aircraftRepository)
    : IRequestHandler<GetConnectedAircraftRequest, GetConnectedAircraftResult>
{
    public async Task<GetConnectedAircraftResult> Handle(
        GetConnectedAircraftRequest request,
        CancellationToken cancellationToken)
    {
        var aircraft = await aircraftRepository.All(cancellationToken);
        var aircraftInfo = aircraft
            .Select(a => new AircraftConnectionDto(
                a.Callsign,
                a.StationId,
                DialogueConverter.ToDto(a.DataAuthorityState)))
            .ToArray();

        return new GetConnectedAircraftResult(aircraftInfo);
    }
}
