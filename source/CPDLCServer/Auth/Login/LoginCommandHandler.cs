using CPDLCServer.Auth.Administrators;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CPDLCServer.Auth.Login;

// Checks the password of an administrator.
// After too many failed logins from an IP address, the login is locked for that IP address.
public class LoginCommandHandler(
    IAdministratorRepository administratorRepository,
    IPasswordHasher<Administrator> passwordHasher,
    LoginLockout loginLockout,
    ILogger logger)
    : IRequestHandler<LoginCommand, LoginResult?>
{
    public async Task<LoginResult?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Do not check the password during the lockout, so that a person cannot continue to try passwords
        if (loginLockout.IsLockedOut(request.IpAddress))
        {
            logger.Warning("Failed login for {Cid} from {IpAddress}: the IP address is locked out", request.Cid, request.IpAddress);
            return null;
        }

        // An administrator without a password (added with VATSIM Connect) cannot log in with a password
        var administrator = await administratorRepository.FindByCid(request.Cid.Trim(), cancellationToken);
        var result = administrator?.PasswordHash is null
            ? PasswordVerificationResult.Failed
            : passwordHasher.VerifyHashedPassword(administrator, administrator.PasswordHash, request.Password);

        if (administrator is null || result == PasswordVerificationResult.Failed)
        {
            var lockoutEnd = loginLockout.RecordFailure(request.IpAddress);
            if (lockoutEnd is not null)
            {
                logger.Warning(
                    "Failed login for {Cid} from {IpAddress}: the IP address is locked out until {LockoutEnd}",
                    request.Cid, request.IpAddress, lockoutEnd);
            }
            else
            {
                logger.Warning("Failed login for {Cid} from {IpAddress}", request.Cid, request.IpAddress);
            }

            return null;
        }

        loginLockout.Reset(request.IpAddress);

        // The hash was made with older settings. Make it again with the current settings.
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            administrator.PasswordHash = passwordHasher.HashPassword(administrator, request.Password);

        logger.Information("Administrator {Cid} logged in from {IpAddress}", administrator.Cid, request.IpAddress);
        return new LoginResult(administrator.Cid);
    }
}
