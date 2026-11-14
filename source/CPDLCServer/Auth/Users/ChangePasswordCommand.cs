using MediatR;

namespace CPDLCServer.Auth.Users;

public record ChangePasswordCommand(Guid UserId, string NewPassword) : IRequest;
