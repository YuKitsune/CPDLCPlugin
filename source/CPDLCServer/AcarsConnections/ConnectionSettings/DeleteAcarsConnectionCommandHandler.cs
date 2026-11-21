using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.AcarsConnections.ConnectionSettings;

public class DeleteAcarsConnectionCommandHandler(
    CpdlcDbContext dbContext,
    IAcarsConnectionManager acarsConnectionManager,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<DeleteAcarsConnectionCommand>
{
    public async Task Handle(DeleteAcarsConnectionCommand request, CancellationToken cancellationToken)
    {
        var settings = await dbContext.AcarsConnections.FindAsync([request.Id], cancellationToken);
        if (settings is null)
        {
            logger.Warning("ACARS connection {Id} not found for deletion", request.Id);
            return;
        }

        dbContext.AcarsConnections.Remove(settings);

        logger.Information("ACARS connection for {StationId} deleted", settings.StationIdentifier);

        unitOfWork.AfterCommit(ct => acarsConnectionManager.Stop(request.Id, ct));
    }
}
