using CPDLCServer.Persistence;
using CPDLCServer.Security;
using MediatR;

namespace CPDLCServer.AcarsConnections.ConnectionSettings;

public class UpdateAcarsConnectionCommandHandler(
    CpdlcDbContext dbContext,
    MasterKeyEncryption masterKeyEncryption,
    IAcarsConnectionManager acarsConnectionManager,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<UpdateAcarsConnectionCommand>
{
    public async Task Handle(UpdateAcarsConnectionCommand request, CancellationToken cancellationToken)
    {
        var settings = await dbContext.AcarsConnections.FindAsync([request.Id], cancellationToken);
        if (settings is null)
            throw new InvalidOperationException($"ACARS connection {request.Id} not found");

        var changes = request.Settings;
        settings.Type = changes.Type;
        settings.Enabled = changes.Enabled;
        settings.StationIdentifier = changes.StationIdentifier.ToUpperInvariant();
        settings.FlightSimulationNetwork = changes.FlightSimulationNetwork;
        settings.Url = changes.Url;
        settings.DefaultAdscReportingInterval = changes.DefaultAdscReportingInterval;
        settings.SelectedAdscContractDuration = changes.SelectedAdscContractDuration;
        settings.Notes = changes.Notes;

        if (!string.IsNullOrWhiteSpace(request.AuthenticationCode))
            settings.EncryptedAuthenticationCode = masterKeyEncryption.Encrypt(request.AuthenticationCode);

        logger.Information("ACARS connection for {StationId} updated", settings.StationIdentifier);

        // Start the connection again, so that it uses the new settings
        unitOfWork.AfterCommit(async ct =>
        {
            await acarsConnectionManager.Stop(settings.Id, ct);

            if (settings.Enabled)
                await acarsConnectionManager.Start(settings, ct);
        });
    }
}
