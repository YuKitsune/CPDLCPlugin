using CPDLCServer.Auth.Administrators;
using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using CPDLCServer.Settings;
using MediatR;

namespace CPDLCServer.Setup;

public class CompleteSetupCommandHandler(
    IAdministratorRepository administratorRepository,
    IServerSettingsProvider settingsProvider,
    SetupState setupState,
    IClock clock,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<CompleteSetupCommand>
{
    public async Task Handle(CompleteSetupCommand request, CancellationToken cancellationToken)
    {
        if (await administratorRepository.Any(cancellationToken))
            throw new InvalidOperationException("An administrator already exists");

        await administratorRepository.Add(
            new Administrator { Cid = request.AdministratorCid, Added = clock.UtcNow() },
            cancellationToken);

        var settings = await settingsProvider.Load(cancellationToken);
        await settingsProvider.Save(
            settings with
            {
                OperatorName = request.OperatorName.Trim(),
                PrivacyContact = request.PrivacyContact.Trim()
            },
            cancellationToken);

        unitOfWork.AfterCommit(_ =>
        {
            setupState.MarkConfigured();
            logger.Information("First administrator {Cid} added. The setup is complete", request.AdministratorCid);
            return Task.CompletedTask;
        });
    }
}
