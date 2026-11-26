using MediatR;

namespace CPDLCServer.Auth.Administrators;

// The name and the password are only for the BuiltIn login. With VATSIM Connect, they are null.
public record AddAdministratorCommand(string Cid, string? Name, string? Password) : IRequest;
