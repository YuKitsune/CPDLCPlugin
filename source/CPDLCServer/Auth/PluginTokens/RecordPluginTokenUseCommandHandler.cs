using CPDLCServer.Infrastructure;
using MediatR;

namespace CPDLCServer.Auth.PluginTokens;

public class RecordPluginTokenUseCommandHandler(IPluginTokenRepository pluginTokenRepository, IClock clock)
    : IRequestHandler<RecordPluginTokenUseCommand>
{
    public async Task Handle(RecordPluginTokenUseCommand request, CancellationToken cancellationToken)
    {
        // An administrator can delete the token at the same time
        var pluginToken = await pluginTokenRepository.FindById(request.Id, cancellationToken);
        if (pluginToken is null)
            return;

        pluginToken.LastUsed = clock.UtcNow();
    }
}
