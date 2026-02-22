using CPDLCServer.Clients;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class LogonCommandHandler(IClientManager clientManager, IAircraftRepository aircraftRepository, IClock clock, IMediator mediator)
    : IRequestHandler<LogonCommand>
{
    public async Task Handle(LogonCommand request, CancellationToken cancellationToken)
    {
        var client = await clientManager.GetAcarsClient(request.AcarsClientId, cancellationToken);

        var aircraft = new AircraftConnection(
            request.Callsign,
            request.AcarsClientId,
            client.StationId,
            DataAuthorityState.NextDataAuthority);

        aircraft.RequestLogon(clock.UtcNow());

        // TODO: Perform validation
        // TODO: What if there are no controllers online?

        await aircraftRepository.Add(
            new(request.Callsign, request.AcarsClientId),
            aircraft,
            cancellationToken);

        // Immediately accept it for now
        aircraft.AcceptLogon(clock.UtcNow());

        await mediator.Send(
            new SendUplinkCommand(
                "SYSTEM",
                request.Callsign,
                request.DownlinkId,
                CpdlcUplinkResponseType.NoResponse,
                "LOGON ACCEPTED"),
            cancellationToken);

        await mediator.Publish(
            new AircraftConnectionEstablished(
                request.AcarsClientId,
                aircraft.StationId,
                request.Callsign,
                aircraft.DataAuthorityState),
            cancellationToken);
    }
}
