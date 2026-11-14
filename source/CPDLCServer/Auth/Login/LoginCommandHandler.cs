using CPDLCServer.Auth.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CPDLCServer.Auth.Login;

// Checks the password of a user.
// After too many failed logins from an IP address, the login is locked for that IP address.
public class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher,
    LoginLockout loginLockout,
    ILogger logger)
    : IRequestHandler<LoginCommand, LoginResult?>
{
    public async Task<LoginResult?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Do not check the password during the lockout, so that a person cannot continue to try passwords
        if (loginLockout.IsLockedOut(request.IpAddress))
        {
            logger.Warning("Failed login for {Username} from {IpAddress}: the IP address is locked out", request.Username, request.IpAddress);
            return null;
        }

        var user = await userRepository.FindByUsername(request.Username.Trim(), cancellationToken);
        var result = user is null
            ? PasswordVerificationResult.Failed
            : passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (user is null || result == PasswordVerificationResult.Failed)
        {
            var lockoutEnd = loginLockout.RecordFailure(request.IpAddress);
            if (lockoutEnd is not null)
            {
                logger.Warning(
                    "Failed login for {Username} from {IpAddress}: the IP address is locked out until {LockoutEnd}",
                    request.Username, request.IpAddress, lockoutEnd);
            }
            else
            {
                logger.Warning("Failed login for {Username} from {IpAddress}", request.Username, request.IpAddress);
            }

            return null;
        }

        loginLockout.Reset(request.IpAddress);

        // The hash was made with older settings. Make it again with the current settings.
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        logger.Information("User {Username} logged in from {IpAddress}", user.Username, request.IpAddress);
        return new LoginResult(user.Id, user.Username);
    }
}
