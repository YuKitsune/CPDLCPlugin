using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Auth.PluginTokens;

// Returns the number of deleted tokens. An ID that does not exist is ignored.
public class DeletePluginTokensCommandHandler(
    IPluginTokenRepository pluginTokenRepository,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<DeletePluginTokensCommand, int>
{
    public async Task<int> Handle(DeletePluginTokensCommand request, CancellationToken cancellationToken)
    {
        var pluginTokens = await pluginTokenRepository.FindByIds(request.Ids, cancellationToken);
        foreach (var pluginToken in pluginTokens)
            await pluginTokenRepository.Remove(pluginToken, cancellationToken);

        unitOfWork.AfterCommit(_ =>
        {
            foreach (var pluginToken in pluginTokens)
            {
                logger.Information(
                    "Plugin token of {Cid} deleted by {AdministratorCid}",
                    pluginToken.Cid, request.AdministratorCid);
            }

            return Task.CompletedTask;
        });

        return pluginTokens.Length;
    }
}
