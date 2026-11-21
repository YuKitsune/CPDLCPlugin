using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Auth.Administrators;

public class RemoveAdministratorCommandHandler(
    IAdministratorRepository administratorRepository,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<RemoveAdministratorCommand>
{
    public async Task Handle(RemoveAdministratorCommand request, CancellationToken cancellationToken)
    {
        if (request.Cid == request.CurrentUserCid)
            throw new InvalidOperationException("You cannot remove yourself");

        var administrator = await administratorRepository.FindByCid(request.Cid, cancellationToken);
        if (administrator is null)
            throw new InvalidOperationException($"{request.Cid} is not an administrator");

        var administrators = await administratorRepository.All(cancellationToken);
        if (administrators.Length <= 1)
            throw new InvalidOperationException("You cannot remove the last administrator");

        await administratorRepository.Remove(administrator, cancellationToken);

        unitOfWork.AfterCommit(_ =>
        {
            logger.Information("Administrator {Cid} removed by {CurrentUserCid}", request.Cid, request.CurrentUserCid);
            return Task.CompletedTask;
        });
    }
}
