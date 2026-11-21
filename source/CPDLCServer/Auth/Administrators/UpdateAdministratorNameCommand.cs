using MediatR;

namespace CPDLCServer.Auth.Administrators;

// Keeps the name of an administrator the same as the name in VATSIM. Does nothing if the user is not an administrator.
public record UpdateAdministratorNameCommand(string Cid, string Name) : IRequest;
