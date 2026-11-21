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
            // Without the banner, so that the image bytes are not in the log
            logger.Information("Server settings changed: {@Settings}", request.Settings with { BannerImage = null });
            return Task.CompletedTask;
        });
    }
}
