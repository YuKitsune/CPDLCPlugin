using System.Security.Cryptography;
using System.Text;

namespace CPDLCServer.Security;

/// <summary>
/// Encrypts data with AES-GCM and the master key, so that the database does not contain them as plain text.
/// The encrypted value is the nonce, the tag and the ciphertext, as Base64 text.
/// If the master key changes, the server cannot decrypt the secrets that were encrypted with the old key.
/// The key comes from the MasterKey environment variable. A missing or invalid key stops the server at startup.
/// </summary>
public class MasterKeyEncryption(string masterKey)
{
    const int KeySize = 32;
    const int NonceSize = 12;
    const int TagSize = 16;

    readonly byte[] _key = ParseKey(masterKey);

    public string Encrypt(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintextBytes.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        return Convert.ToBase64String([..nonce, ..tag, ..ciphertext]);
    }

    public string Decrypt(string encrypted)
    {
        var bytes = Convert.FromBase64String(encrypted);
        if (bytes.Length < NonceSize + TagSize)
            throw new CryptographicException("The encrypted value is too short");

        var nonce = bytes.AsSpan(0, NonceSize);
        var tag = bytes.AsSpan(NonceSize, TagSize);
        var ciphertext = bytes.AsSpan(NonceSize + TagSize);
        var plaintextBytes = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    static byte[] ParseKey(string? masterKey)
    {
        if (string.IsNullOrWhiteSpace(masterKey))
            throw new Exception("The MasterKey environment variable is not set. Make a key with: openssl rand -base64 32");

        byte[] key;
        try
        {
            key = Convert.FromBase64String(masterKey);
        }
        catch (FormatException ex)
        {
            throw new Exception("The MasterKey environment variable is not valid Base64. Make a key with: openssl rand -base64 32", ex);
        }

        if (key.Length != KeySize)
            throw new Exception($"The MasterKey environment variable must contain a key of {KeySize} bytes. Make a key with: openssl rand -base64 32");

        return key;
    }
}
