using CPDLCServer.Auth.Administrators;
using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using CPDLCServer.Settings;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CPDLCServer.Setup;

public class CompleteSetupCommandHandler(
    IAdministratorRepository administratorRepository,
    IPasswordHasher<Administrator> passwordHasher,
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

        var administrator = new Administrator
        {
            Cid = request.AdministratorCid,
            Name = string.IsNullOrWhiteSpace(request.AdministratorName) ? null : request.AdministratorName.Trim(),
            Added = clock.UtcNow()
        };

        if (request.AdministratorPassword is not null)
            administrator.PasswordHash = passwordHasher.HashPassword(administrator, request.AdministratorPassword);

        await administratorRepository.Add(administrator, cancellationToken);

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
