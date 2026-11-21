using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Settings;

public class SetBannerImageCommandHandler(
    IServerSettingsProvider settingsProvider,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<SetBannerImageCommand>
{
    public async Task Handle(SetBannerImageCommand request, CancellationToken cancellationToken)
    {
        string? contentType = null;
        if (request.Image is not null)
        {
            if (request.Image.Length > BannerImageFormat.MaxSize)
                throw new InvalidOperationException("The image is larger than 1 MB");

            contentType = BannerImageFormat.FindContentType(request.Image)
                ?? throw new InvalidOperationException("The image must be PNG, JPEG or WebP");
        }

        var settings = await settingsProvider.Load(cancellationToken);
        settings.BannerImage = request.Image;
        settings.BannerContentType = contentType;
        settings.BannerVersion++;

        unitOfWork.AfterCommit(_ =>
        {
            logger.Information(request.Image is null ? "Banner removed" : "Banner changed");
            return Task.CompletedTask;
        });
    }
}
