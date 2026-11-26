using MediatR;

namespace CPDLCServer.Auth.Login;

// The BuiltIn login of an administrator. Returns null when the login fails.
public record LoginCommand(string Cid, string Password, string IpAddress) : IRequest<LoginResult?>;

public record LoginResult(string Cid, string Name);
