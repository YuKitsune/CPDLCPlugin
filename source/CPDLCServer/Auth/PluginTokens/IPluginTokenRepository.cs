namespace CPDLCServer.Auth.PluginTokens;

public interface IPluginTokenRepository
{
    Task Add(PluginToken token, CancellationToken cancellationToken);
    Task Remove(PluginToken token, CancellationToken cancellationToken);
    Task<PluginToken?> FindById(Guid id, CancellationToken cancellationToken);
    Task<PluginToken?> FindByHash(string tokenHash, CancellationToken cancellationToken);
    Task<PluginToken[]> FindByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    // The tokens whose CID starts with the prefix, newest first. A null prefix gives all tokens.
    Task<PluginToken[]> Search(string? cidPrefix, int skip, int take, CancellationToken cancellationToken);
    Task<int> Count(string? cidPrefix, CancellationToken cancellationToken);
}
