using MediatR;

namespace CPDLCServer.Auth.Administrators;

// The password is only for the BuiltIn login. With VATSIM Connect, it is null.
public record AddAdministratorCommand(string Cid, string? Password) : IRequest;
