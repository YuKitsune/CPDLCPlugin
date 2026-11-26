using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CPDLCServer.Auth.Administrators;

public class ChangePasswordCommandHandler(
    IAdministratorRepository administratorRepository,
    IPasswordHasher<Administrator> passwordHasher,
    ILogger logger)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var administrator = await administratorRepository.FindByCid(request.Cid, cancellationToken);
        if (administrator is null)
            throw new InvalidOperationException($"{request.Cid} is not an administrator");

        administrator.PasswordHash = passwordHasher.HashPassword(administrator, request.NewPassword);
        logger.Information("The password of administrator {Cid} was changed", request.Cid);
    }
}
