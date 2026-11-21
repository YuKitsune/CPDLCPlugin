using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Auth.Administrators;

public class AddAdministratorCommandHandler(
    IAdministratorRepository administratorRepository,
    IClock clock,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<AddAdministratorCommand>
{
    public async Task Handle(AddAdministratorCommand request, CancellationToken cancellationToken)
    {
        if (await administratorRepository.FindByCid(request.Cid, cancellationToken) is not null)
            throw new InvalidOperationException($"{request.Cid} is already an administrator");

        await administratorRepository.Add(
            new Administrator { Cid = request.Cid, Added = clock.UtcNow() },
            cancellationToken);

        unitOfWork.AfterCommit(_ =>
        {
            logger.Information("Administrator {Cid} added", request.Cid);
            return Task.CompletedTask;
        });
    }
}
