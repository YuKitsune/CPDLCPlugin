namespace CPDLCServer.Auth.PluginTokens;

public interface IPluginTokenRepository
{
    Task Add(PluginToken token, CancellationToken cancellationToken);
    Task Remove(PluginToken token, CancellationToken cancellationToken);
    Task<PluginToken?> FindByHash(string tokenHash, CancellationToken cancellationToken);
}
