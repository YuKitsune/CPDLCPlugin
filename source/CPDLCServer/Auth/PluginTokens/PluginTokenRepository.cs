using CPDLCServer.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Auth.PluginTokens;

// The changes are saved when the unit of work is committed
public class PluginTokenRepository(CpdlcDbContext dbContext) : IPluginTokenRepository
{
    public Task Add(PluginToken token, CancellationToken cancellationToken)
    {
        dbContext.PluginTokens.Add(token);
        return Task.CompletedTask;
    }

    public Task Remove(PluginToken token, CancellationToken cancellationToken)
    {
        dbContext.PluginTokens.Remove(token);
        return Task.CompletedTask;
    }

    public async Task<PluginToken?> FindByHash(string tokenHash, CancellationToken cancellationToken)
    {
        return await dbContext.PluginTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }
}
