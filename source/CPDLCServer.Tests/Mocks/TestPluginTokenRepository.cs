using CPDLCServer.Auth.PluginTokens;

namespace CPDLCServer.Tests.Mocks;

public class TestPluginTokenRepository : IPluginTokenRepository
{
    private readonly List<PluginToken> _tokens = [];

    public Task Add(PluginToken token, CancellationToken cancellationToken)
    {
        _tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task Remove(PluginToken token, CancellationToken cancellationToken)
    {
        _tokens.Remove(token);
        return Task.CompletedTask;
    }

    public Task<PluginToken?> FindById(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_tokens.FirstOrDefault(t => t.Id == id));
    }

    public Task<PluginToken?> FindByHash(string tokenHash, CancellationToken cancellationToken)
    {
        return Task.FromResult(_tokens.FirstOrDefault(t => t.TokenHash == tokenHash));
    }

    public Task<PluginToken[]> FindByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        return Task.FromResult(_tokens.Where(t => ids.Contains(t.Id)).ToArray());
    }

    public Task<PluginToken[]> Search(string? cidPrefix, int skip, int take, CancellationToken cancellationToken)
    {
        return Task.FromResult(Filter(cidPrefix).OrderByDescending(t => t.Created).Skip(skip).Take(take).ToArray());
    }

    public Task<int> Count(string? cidPrefix, CancellationToken cancellationToken)
    {
        return Task.FromResult(Filter(cidPrefix).Count());
    }

    IEnumerable<PluginToken> Filter(string? cidPrefix)
    {
        return string.IsNullOrEmpty(cidPrefix) ? _tokens : _tokens.Where(t => t.Cid.StartsWith(cidPrefix, StringComparison.Ordinal));
    }
}
