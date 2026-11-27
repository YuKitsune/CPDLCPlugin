using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using vatsys;

namespace CPDLCPlugin.Server;

// A token that the plugin uses to authenticate with a CPDLC server
public record AuthenticationToken(string AccessToken, string Cid, DateTimeOffset Expires);

/// <summary>
/// Keeps the token for each CPDLC server, so that the user does not have to authenticate each time that vatSys starts.
/// The access tokens are encrypted with the Windows account of the user (DPAPI), so a different account cannot read them.
/// An expired token, or a token that the store cannot read, is deleted.
/// </summary>
public class AuthenticationStore
{
    readonly string _filePath = Path.Combine(Helpers.GetFilesFolder(), "cpdlc_tokens.json");
    readonly object _lock = new();

    // Returns null if there is no token for the server, or if the token is expired
    public AuthenticationToken? Get(Uri server)
    {
        lock (_lock)
        {
            var tokens = Load();
            var savedToken = tokens.FirstOrDefault(t => t.Server == Key(server));
            if (savedToken is null)
                return null;

            if (savedToken.Expires <= DateTimeOffset.UtcNow || !TryDecrypt(savedToken.EncryptedAccessToken, out var accessToken))
            {
                Write(tokens.Where(t => t != savedToken).ToArray());
                return null;
            }

            return new AuthenticationToken(accessToken, savedToken.Cid, savedToken.Expires);
        }
    }

    public void Save(Uri server, AuthenticationToken token)
    {
        lock (_lock)
        {
            var savedToken = new SavedToken(Key(server), Encrypt(token.AccessToken), token.Cid, token.Expires);
            var tokens = Load()
                .Where(t => t.Server != savedToken.Server)
                .Append(savedToken)
                .ToArray();

            Write(tokens);
        }
    }

    public void Delete(Uri server)
    {
        lock (_lock)
        {
            var tokens = Load();
            var remainingTokens = tokens.Where(t => t.Server != Key(server)).ToArray();
            if (remainingTokens.Length != tokens.Length)
                Write(remainingTokens);
        }
    }

    // A file that the store cannot read is deleted
    SavedToken[] Load()
    {
        if (!File.Exists(_filePath))
            return [];

        try
        {
            var tokenFile = JsonSerializer.Deserialize<TokenFile>(File.ReadAllText(_filePath));
            if (tokenFile?.Tokens is { } tokens && tokens.All(IsValid))
                return tokens;
        }
        catch (JsonException)
        {
        }

        File.Delete(_filePath);
        return [];
    }

    void Write(SavedToken[] tokens)
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(new TokenFile(tokens)));
    }

    static bool IsValid(SavedToken? token)
    {
        return token is not null &&
               !string.IsNullOrEmpty(token.Server) &&
               !string.IsNullOrEmpty(token.EncryptedAccessToken) &&
               !string.IsNullOrEmpty(token.Cid);
    }

    static string Encrypt(string accessToken)
    {
        var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(accessToken), null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(bytes);
    }

    // Returns false if a different Windows account encrypted the token, or if the token is not valid Base64
    static bool TryDecrypt(string encryptedAccessToken, out string accessToken)
    {
        try
        {
            var bytes = ProtectedData.Unprotect(Convert.FromBase64String(encryptedAccessToken), null, DataProtectionScope.CurrentUser);
            accessToken = Encoding.UTF8.GetString(bytes);
            return true;
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            accessToken = string.Empty;
            return false;
        }
    }

    static string Key(Uri server) => server.GetLeftPart(UriPartial.Authority).ToLowerInvariant();

    // The contents of the token file
    record TokenFile(SavedToken[] Tokens);

    // Only the access token is secret, so only the access token is encrypted
    record SavedToken(string Server, string EncryptedAccessToken, string Cid, DateTimeOffset Expires);
}
