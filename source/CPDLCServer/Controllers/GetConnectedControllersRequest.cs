using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.Controllers;

public record GetConnectedControllersRequest : IRequest<GetConnectedControllersResult>;

public record GetConnectedControllersResult(ControllerConnectionDto[] Controllers);
