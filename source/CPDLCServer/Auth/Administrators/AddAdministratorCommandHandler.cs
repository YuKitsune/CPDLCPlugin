using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CPDLCServer.Auth.Administrators;

public class AddAdministratorCommandHandler(
    IAdministratorRepository administratorRepository,
    IPasswordHasher<Administrator> passwordHasher,
    IClock clock,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<AddAdministratorCommand>
{
    public async Task Handle(AddAdministratorCommand request, CancellationToken cancellationToken)
    {
        if (await administratorRepository.FindByCid(request.Cid, cancellationToken) is not null)
            throw new InvalidOperationException($"{request.Cid} is already an administrator");

        var administrator = new Administrator
        {
            Cid = request.Cid,
            Added = clock.UtcNow()
        };

        if (request.Password is not null)
            administrator.PasswordHash = passwordHasher.HashPassword(administrator, request.Password);

        await administratorRepository.Add(administrator, cancellationToken);

        unitOfWork.AfterCommit(_ =>
        {
            logger.Information("Administrator {Cid} added", request.Cid);
            return Task.CompletedTask;
        });
    }
}
