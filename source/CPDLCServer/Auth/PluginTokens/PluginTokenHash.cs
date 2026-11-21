using System.Security.Cryptography;
using System.Text;

namespace CPDLCServer.Auth.PluginTokens;

public static class PluginTokenHash
{
    // The token is random and long, so a fast hash is safe. A slow hash, as for a password, is not necessary.
    public static string Compute(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public static string NewToken()
    {
        return Base64Url(RandomNumberGenerator.GetBytes(32));
    }

    public static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
