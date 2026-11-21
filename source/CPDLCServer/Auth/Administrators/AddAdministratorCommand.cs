using MediatR;

namespace CPDLCServer.Auth.Administrators;

public record AddAdministratorCommand(int Cid) : IRequest;
