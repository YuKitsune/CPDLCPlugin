using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.Controllers;

public class GetConnectedControllersRequestHandler(IControllerRepository controllerRepository)
    : IRequestHandler<GetConnectedControllersRequest, GetConnectedControllersResult>
{
    public async Task<GetConnectedControllersResult> Handle(
        GetConnectedControllersRequest request,
        CancellationToken cancellationToken)
    {
        var controllers = await controllerRepository.All(cancellationToken);

        var controllerInfo = controllers
            .Select(c => new ControllerConnectionDto(c.StationId, c.Callsign))
            .ToArray();

        return new GetConnectedControllersResult(controllerInfo);
    }
}
