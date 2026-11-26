using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Auth.PluginTokens;

public class RevokePluginTokenCommandHandler(
    IPluginTokenRepository pluginTokenRepository,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<RevokePluginTokenCommand>
{
    public async Task Handle(RevokePluginTokenCommand request, CancellationToken cancellationToken)
    {
        var pluginToken = await pluginTokenRepository.FindByHash(PluginTokenHash.Compute(request.Token), cancellationToken);
        if (pluginToken is null)
            return;

        await pluginTokenRepository.Remove(pluginToken, cancellationToken);

        unitOfWork.AfterCommit(_ =>
        {
            logger.Information("Plugin token revoked for {Cid}", pluginToken.Cid);
            return Task.CompletedTask;
        });
    }
}
