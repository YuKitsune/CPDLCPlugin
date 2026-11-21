using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using CPDLCServer.Setup;
using MediatR;

namespace CPDLCServer.Auth.Administrators;

public class CreateFirstAdministratorCommandHandler(
    IAdministratorRepository administratorRepository,
    SetupState setupState,
    IClock clock,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<CreateFirstAdministratorCommand>
{
    public async Task Handle(CreateFirstAdministratorCommand request, CancellationToken cancellationToken)
    {
        if (await administratorRepository.Any(cancellationToken))
            throw new InvalidOperationException("An administrator already exists");

        await administratorRepository.Add(
            new Administrator { Cid = request.Cid, Added = clock.UtcNow() },
            cancellationToken);

        unitOfWork.AfterCommit(_ =>
        {
            setupState.MarkConfigured();
            logger.Information("First administrator {Cid} added. The setup is complete", request.Cid);
            return Task.CompletedTask;
        });
    }
}
