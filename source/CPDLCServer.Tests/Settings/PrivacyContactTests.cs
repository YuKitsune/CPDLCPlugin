using CPDLCServer.Settings;

namespace CPDLCServer.Tests.Settings;

public class PrivacyContactTests
{
    [Theory]
    [InlineData("privacy@example.com", true)]
    [InlineData("https://example.com/privacy", true)]
    [InlineData("http://example.com/privacy", true)]
    [InlineData("", true)]
    [InlineData(null, true)]
    [InlineData("not a contact", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("ftp://example.com", false)]
    [InlineData("Privacy <privacy@example.com>", false)]
    public void IsValid_AcceptsAnEmailAddressOrAWebUrl(string? contact, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, new PrivacyContactAttribute().IsValid(contact));
    }

    [Theory]
    [InlineData("privacy@example.com", "mailto:privacy@example.com")]
    [InlineData("https://example.com/privacy", "https://example.com/privacy")]
    public void ToHref_MakesALink(string contact, string expected)
    {
        // Act & Assert
        Assert.Equal(expected, PrivacyContactFormat.ToHref(contact));
    }
}
