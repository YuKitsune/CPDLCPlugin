namespace CPDLCServer.Auth.PluginTokens;

// Lets the plugin of one VATSIM user connect to the hub. The plain token is never stored, only its hash.
public class PluginToken
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    public Guid Id { get; init; }
    public int Cid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string TokenHash { get; init; } = string.Empty;
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset Expires { get; init; }
}
