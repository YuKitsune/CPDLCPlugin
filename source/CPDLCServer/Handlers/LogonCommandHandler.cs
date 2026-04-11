using CPDLCServer.Clients;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class LogonCommandHandler(IClientManager clientManager, IAircraftRepository aircraftRepository, IControllerRepository controllerRepository, IClock clock, IMediator mediator)
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

        await aircraftRepository.Add(
            new(request.Callsign, request.AcarsClientId),
            aircraft,
            cancellationToken);

        // Don't accept the logon if no controllers are connected
        var activeControllers = await controllerRepository.All(cancellationToken);
        if (activeControllers.Length == 0)
        {
            await mediator.Send(
                new SendUplinkCommand(
                    "SYSTEM",
                    request.Callsign,
                    ReplyToDownlinkId: request.DownlinkId,
                    CpdlcUplinkResponseType.NoResponse,
                    "LOGON REJECTED. NO ATS AVBL."),
                cancellationToken);
            return;
        }

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
