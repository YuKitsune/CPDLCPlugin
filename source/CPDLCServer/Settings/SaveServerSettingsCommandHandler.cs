using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Settings;

public class SaveServerSettingsCommandHandler(
    IServerSettingsProvider settingsProvider,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<SaveServerSettingsCommand>
{
    public async Task Handle(SaveServerSettingsCommand request, CancellationToken cancellationToken)
    {
        await settingsProvider.Save(request.Settings, cancellationToken);

        unitOfWork.AfterCommit(_ =>
        {
            logger.Information("Server settings changed: {@Settings}", request.Settings);
            return Task.CompletedTask;
        });
    }
}
