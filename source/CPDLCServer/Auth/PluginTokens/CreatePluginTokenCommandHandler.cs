using CPDLCServer.Contracts;
using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Auth.PluginTokens;

public class CreatePluginTokenCommandHandler(
    IPluginTokenRepository pluginTokenRepository,
    IClock clock,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<CreatePluginTokenCommand, PluginTokenResponse>
{
    public async Task<PluginTokenResponse> Handle(CreatePluginTokenCommand request, CancellationToken cancellationToken)
    {
        var token = PluginTokenHash.NewToken();
        var now = clock.UtcNow();

        var pluginToken = new PluginToken
        {
            Id = Guid.NewGuid(),
            Cid = request.Cid,
            Name = request.Name,
            TokenHash = PluginTokenHash.Compute(token),
            Created = now,
            Expires = now + PluginToken.Lifetime
        };

        await pluginTokenRepository.Add(pluginToken, cancellationToken);

        unitOfWork.AfterCommit(_ =>
        {
            logger.Information("Plugin token created for {Name} ({Cid})", request.Name, request.Cid);
            return Task.CompletedTask;
        });

        return new PluginTokenResponse(token, pluginToken.Expires, request.Cid, request.Name);
    }
}
