using MediatR;

namespace CPDLCServer.Auth.Login;

// Returns null when the login fails
public record LoginCommand(string Username, string Password, string IpAddress) : IRequest<LoginResult?>;

public record LoginResult(Guid UserId, string Username);
