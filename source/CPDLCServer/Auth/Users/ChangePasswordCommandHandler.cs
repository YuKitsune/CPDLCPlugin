using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CPDLCServer.Auth.Users;

public class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher,
    ILogger logger)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindById(request.UserId, cancellationToken);
        if (user is null)
            throw new InvalidOperationException($"User {request.UserId} does not exist");

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        logger.Information("The password of user {Username} was changed", user.Username);
    }
}
