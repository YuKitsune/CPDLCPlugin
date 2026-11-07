using CPDLCServer.Persistence;
using CPDLCServer.Security;
using MediatR;

namespace CPDLCServer.AcarsConnections.ConnectionSettings;

public class CreateAcarsConnectionCommandHandler(
    CpdlcDbContext dbContext,
    MasterKeyEncryption masterKeyEncryption,
    IAcarsConnectionManager acarsConnectionManager,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<CreateAcarsConnectionCommand, Guid>
{
    public Task<Guid> Handle(CreateAcarsConnectionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AuthenticationCode))
            throw new ArgumentException("An authentication code is required for a new ACARS connection");

        var settings = request.Settings;
        settings.StationIdentifier = settings.StationIdentifier.ToUpperInvariant();
        settings.EncryptedAuthenticationCode = masterKeyEncryption.Encrypt(request.AuthenticationCode);

        dbContext.AcarsConnections.Add(settings);

        logger.Information("ACARS connection for {StationId} created", settings.StationIdentifier);

        if (settings.Enabled)
            unitOfWork.AfterCommit(ct => acarsConnectionManager.Start(settings, ct));

        return Task.FromResult(settings.Id);
    }
}
