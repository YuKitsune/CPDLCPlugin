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

    public async Task<PluginToken?> FindById(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.PluginTokens.FindAsync([id], cancellationToken);
    }

    public async Task<PluginToken?> FindByHash(string tokenHash, CancellationToken cancellationToken)
    {
        return await dbContext.PluginTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<PluginToken[]> FindByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        return await dbContext.PluginTokens.Where(t => ids.Contains(t.Id)).ToArrayAsync(cancellationToken);
    }

    public async Task<PluginToken[]> Search(string? cidPrefix, int skip, int take, CancellationToken cancellationToken)
    {
        return await Filter(cidPrefix)
            .OrderByDescending(t => t.Created)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<int> Count(string? cidPrefix, CancellationToken cancellationToken)
    {
        return await Filter(cidPrefix).CountAsync(cancellationToken);
    }

    IQueryable<PluginToken> Filter(string? cidPrefix)
    {
        return string.IsNullOrEmpty(cidPrefix)
            ? dbContext.PluginTokens
            : dbContext.PluginTokens.Where(t => t.Cid.StartsWith(cidPrefix));
    }
}
