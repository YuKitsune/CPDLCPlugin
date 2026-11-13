using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using CPDLCServer.Setup;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CPDLCServer.Auth.Users;

// The setup creates the first user
public class CreateUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher,
    SetupState setupState,
    IClock clock,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.Any(cancellationToken))
            throw new InvalidOperationException("A user already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username.Trim(),
            Created = clock.UtcNow()
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await userRepository.Add(user, cancellationToken);

        unitOfWork.AfterCommit(_ =>
        {
            setupState.MarkAdminCreated();
            logger.Information("User {Username} created", user.Username);
            return Task.CompletedTask;
        });

        return user.Id;
    }
}
