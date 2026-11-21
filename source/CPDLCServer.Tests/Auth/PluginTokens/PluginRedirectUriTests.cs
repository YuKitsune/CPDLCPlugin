using CPDLCServer.Auth.PluginTokens;

namespace CPDLCServer.Tests.Auth.PluginTokens;

public class PluginRedirectUriTests
{
    [Theory]
    [InlineData("http://127.0.0.1:50000/callback", true)]
    [InlineData("http://localhost:50000/callback", true)]
    [InlineData("https://127.0.0.1:50000/callback", false)]
    [InlineData("http://127.0.0.1/callback", false)]
    [InlineData("http://example.com:50000/callback", false)]
    [InlineData("http://127.0.0.1.example.com:50000/callback", false)]
    [InlineData("http://127.0.0.1:50000/callback?next=http://example.com", false)]
    [InlineData("http://user@127.0.0.1:50000/callback", false)]
    [InlineData("not a URL", false)]
    [InlineData(null, false)]
    public void IsValid_AcceptsOnlyAPortOnThisComputer(string? redirectUri, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, PluginRedirectUri.IsValid(redirectUri));
    }
}
