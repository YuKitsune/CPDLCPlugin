using System.Security.Cryptography;
using CPDLCServer.Security;

namespace CPDLCServer.Tests.Security;

public class MasterKeyEncryptionTests
{
    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsThePlaintext()
    {
        // Arrange
        var encryption = new MasterKeyEncryption(NewKey());

        // Act
        var encrypted = encryption.Encrypt("secret");

        // Assert
        Assert.NotEqual("secret", encrypted);
        Assert.Equal("secret", encryption.Decrypt(encrypted));
    }

    [Fact]
    public void Encrypt_SameValueTwice_GivesDifferentResults()
    {
        // Arrange
        var encryption = new MasterKeyEncryption(NewKey());

        // Act & Assert
        Assert.NotEqual(encryption.Encrypt("secret"), encryption.Encrypt("secret"));
    }

    [Fact]
    public void Decrypt_WithDifferentKey_Throws()
    {
        // Arrange
        var encrypted = new MasterKeyEncryption(NewKey()).Encrypt("secret");
        var otherEncryption = new MasterKeyEncryption(NewKey());

        // Act & Assert
        Assert.ThrowsAny<CryptographicException>(() => otherEncryption.Decrypt(encrypted));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-key")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA==")]
    public void Constructor_WithMissingOrInvalidKey_Throws(string? masterKey)
    {
        Assert.Throws<Exception>(() => new MasterKeyEncryption(masterKey!));
    }

    static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
