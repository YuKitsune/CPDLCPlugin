using MediatR;

namespace CPDLCServer.Auth.Users;

public record CreateUserCommand(string Username, string Password) : IRequest<Guid>;
