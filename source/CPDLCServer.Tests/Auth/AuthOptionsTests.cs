using CPDLCServer.Auth;
using Microsoft.Extensions.Configuration;

namespace CPDLCServer.Tests.Auth;

public class AuthOptionsTests
{
    [Theory]
    [InlineData("BuiltIn", AuthType.BuiltIn)]
    [InlineData("builtin", AuthType.BuiltIn)]
    [InlineData("VATSIM", AuthType.Vatsim)]
    [InlineData("Vatsim", AuthType.Vatsim)]
    public void FromConfiguration_ReadsTheAuthType(string value, AuthType expected)
    {
        // Act
        var options = AuthOptions.FromConfiguration(CreateConfiguration(value));

        // Assert
        Assert.Equal(expected, options.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Password")]
    [InlineData("0")]
    public void FromConfiguration_WithoutAValidAuthType_Throws(string? value)
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => AuthOptions.FromConfiguration(CreateConfiguration(value)));
        Assert.Contains("AuthType", exception.Message);
    }

    static IConfiguration CreateConfiguration(string? value)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AuthType"] = value })
            .Build();
    }
}
