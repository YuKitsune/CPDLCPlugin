using CPDLCServer.Contracts;
using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class GetConnectedControllersRequestHandler(IControllerRepository controllerRepository)
    : IRequestHandler<GetConnectedControllersRequest, GetConnectedControllersResult>
{
    public async Task<GetConnectedControllersResult> Handle(
        GetConnectedControllersRequest request,
        CancellationToken cancellationToken)
    {
        var controllers = await controllerRepository.All(cancellationToken);

        var controllerInfo = controllers
            .Select(c => new ControllerConnectionDto(c.StationId, c.Callsign, c.VatsimCid))
            .ToArray();

        return new GetConnectedControllersResult(controllerInfo);
    }
}
