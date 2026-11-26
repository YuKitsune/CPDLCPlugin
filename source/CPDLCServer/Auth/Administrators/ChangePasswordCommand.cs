using MediatR;

namespace CPDLCServer.Auth.Administrators;

// Only for the BuiltIn login
public record ChangePasswordCommand(string Cid, string NewPassword) : IRequest;
