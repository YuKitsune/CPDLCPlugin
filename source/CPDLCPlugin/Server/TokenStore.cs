using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using vatsys;

namespace CPDLCPlugin.Server;

/// <summary>
/// Keeps the token for each CPDLC server, so that the user does not have to authenticate each time that vatSys starts.
/// The tokens are encrypted with the Windows account of the user (DPAPI), so a different account cannot read them.
/// </summary>
public class TokenStore
{
    readonly string _filePath = Path.Combine(Helpers.GetFilesFolder(), "cpdlc_tokens.json");
    readonly object _lock = new();

    public string? Get(Uri server)
    {
        lock (_lock)
        {
            var tokens = Load();
            if (!tokens.TryGetValue(Key(server), out var encryptedToken))
                return null;

            try
            {
                var bytes = ProtectedData.Unprotect(Convert.FromBase64String(encryptedToken), null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (CryptographicException)
            {
                // A different Windows account encrypted the token
                return null;
            }
        }
    }

    public void Save(Uri server, string token)
    {
        lock (_lock)
        {
            var tokens = Load();
            var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), null, DataProtectionScope.CurrentUser);
            tokens[Key(server)] = Convert.ToBase64String(bytes);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(tokens));
        }
    }

    public void Delete(Uri server)
    {
        lock (_lock)
        {
            var tokens = Load();
            if (tokens.Remove(Key(server)))
                File.WriteAllText(_filePath, JsonSerializer.Serialize(tokens));
        }
    }

    Dictionary<string, string> Load()
    {
        if (!File.Exists(_filePath))
            return new Dictionary<string, string>();

        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_filePath))
               ?? new Dictionary<string, string>();
    }

    static string Key(Uri server) => server.GetLeftPart(UriPartial.Authority).ToLowerInvariant();
}
