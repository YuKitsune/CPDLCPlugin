using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using CPDLCServer.Infrastructure;

namespace CPDLCServer.Auth.PluginTokens;

/// <summary>
/// The one-use codes of the plugin login (OAuth authorization code with PKCE).
/// The browser gives the code to the plugin. The plugin changes the code for a token, with the verifier that only it has.
/// The codes are kept only in memory. They expire after one minute.
/// </summary>
public class PluginAuthorizationCodes(IClock clock)
{
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(1);

    readonly ConcurrentDictionary<string, PendingCode> _codes = new();

    public string Create(int cid, string name, string redirectUri, string codeChallenge)
    {
        RemoveExpired();

        var code = PluginTokenHash.Base64Url(RandomNumberGenerator.GetBytes(32));
        _codes[code] = new PendingCode(cid, name, redirectUri, codeChallenge, clock.UtcNow() + CodeLifetime);
        return code;
    }

    // Returns null when the code does not exist, has expired, or does not match the redirect URI and the verifier.
    // A code can be used one time only, also when the check fails.
    public PluginUser? Redeem(string code, string redirectUri, string codeVerifier)
    {
        if (!_codes.TryRemove(code, out var pendingCode))
            return null;

        if (pendingCode.Expires <= clock.UtcNow())
            return null;

        if (pendingCode.RedirectUri != redirectUri)
            return null;

        if (!IsValidVerifier(codeVerifier, pendingCode.CodeChallenge))
            return null;

        return new PluginUser(pendingCode.Cid, pendingCode.Name);
    }

    // S256: the challenge is the Base64 URL of the SHA-256 of the verifier
    public static string ComputeChallenge(string codeVerifier)
    {
        return PluginTokenHash.Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
    }

    static bool IsValidVerifier(string codeVerifier, string codeChallenge)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(ComputeChallenge(codeVerifier)),
            Encoding.ASCII.GetBytes(codeChallenge));
    }

    void RemoveExpired()
    {
        var now = clock.UtcNow();
        foreach (var (code, pendingCode) in _codes)
        {
            if (pendingCode.Expires <= now)
                _codes.TryRemove(code, out _);
        }
    }

    record PendingCode(int Cid, string Name, string RedirectUri, string CodeChallenge, DateTimeOffset Expires);
}

public record PluginUser(int Cid, string Name);
