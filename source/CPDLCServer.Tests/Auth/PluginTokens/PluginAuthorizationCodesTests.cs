using CPDLCServer.Auth.PluginTokens;
using CPDLCServer.Tests.Mocks;

namespace CPDLCServer.Tests.Auth.PluginTokens;

public class PluginAuthorizationCodesTests
{
    const string RedirectUri = "http://127.0.0.1:50000/callback";
    const string CodeVerifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

    readonly TestClock _clock = new();

    [Fact]
    public void ComputeChallenge_MatchesTheExampleOfRfc7636()
    {
        // Act
        var challenge = PluginAuthorizationCodes.ComputeChallenge(CodeVerifier);

        // Assert
        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", challenge);
    }

    [Fact]
    public void Redeem_WithTheVerifier_ReturnsTheCid()
    {
        // Arrange
        var codes = new PluginAuthorizationCodes(_clock);
        var code = codes.Create("1234567", RedirectUri, PluginAuthorizationCodes.ComputeChallenge(CodeVerifier));

        // Act
        var cid = codes.Redeem(code, RedirectUri, CodeVerifier);

        // Assert
        Assert.Equal("1234567", cid);
    }

    [Fact]
    public void Redeem_WithAWrongVerifier_ReturnsNull()
    {
        // Arrange
        var codes = new PluginAuthorizationCodes(_clock);
        var code = codes.Create("1234567", RedirectUri, PluginAuthorizationCodes.ComputeChallenge(CodeVerifier));

        // Act
        var cid = codes.Redeem(code, RedirectUri, "wrong-verifier");

        // Assert
        Assert.Null(cid);
    }

    [Fact]
    public void Redeem_WithADifferentRedirectUri_ReturnsNull()
    {
        // Arrange
        var codes = new PluginAuthorizationCodes(_clock);
        var code = codes.Create("1234567", RedirectUri, PluginAuthorizationCodes.ComputeChallenge(CodeVerifier));

        // Act
        var cid = codes.Redeem(code, "http://127.0.0.1:50001/callback", CodeVerifier);

        // Assert
        Assert.Null(cid);
    }

    [Fact]
    public void Redeem_AfterTheCodeExpires_ReturnsNull()
    {
        // Arrange
        var codes = new PluginAuthorizationCodes(_clock);
        var code = codes.Create("1234567", RedirectUri, PluginAuthorizationCodes.ComputeChallenge(CodeVerifier));
        _clock.SetUtcNow(_clock.UtcNow() + PluginAuthorizationCodes.CodeLifetime);

        // Act
        var cid = codes.Redeem(code, RedirectUri, CodeVerifier);

        // Assert
        Assert.Null(cid);
    }

    [Fact]
    public void Redeem_TwoTimes_ReturnsNullTheSecondTime()
    {
        // Arrange
        var codes = new PluginAuthorizationCodes(_clock);
        var code = codes.Create("1234567", RedirectUri, PluginAuthorizationCodes.ComputeChallenge(CodeVerifier));
        codes.Redeem(code, RedirectUri, CodeVerifier);

        // Act
        var cid = codes.Redeem(code, RedirectUri, CodeVerifier);

        // Assert
        Assert.Null(cid);
    }

    [Fact]
    public void Redeem_AfterAFailedCheck_ReturnsNull()
    {
        // Arrange
        var codes = new PluginAuthorizationCodes(_clock);
        var code = codes.Create("1234567", RedirectUri, PluginAuthorizationCodes.ComputeChallenge(CodeVerifier));
        codes.Redeem(code, RedirectUri, "wrong-verifier");

        // Act
        var cid = codes.Redeem(code, RedirectUri, CodeVerifier);

        // Assert
        Assert.Null(cid);
    }
}
