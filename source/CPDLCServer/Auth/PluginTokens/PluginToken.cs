namespace CPDLCServer.Auth.PluginTokens;

// Lets the plugin of one controller connect to the hub. One CID can have many tokens. The plain token is never stored, only its hash.
public class PluginToken
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    public Guid Id { get; init; }
    public required string Cid { get; init; }
    public string TokenHash { get; init; } = string.Empty;
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset Expires { get; init; }

    // The time of the latest request with this token. Null when the token was not used.
    public DateTimeOffset? LastUsed { get; set; }

    // The server records the use not more than one time in this period, so that each request does not write to the database
    public static readonly TimeSpan LastUsedPrecision = TimeSpan.FromMinutes(1);
}
