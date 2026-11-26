using CPDLCServer.AcarsConnections.Hoppie;

namespace CPDLCServer.Tests.AcarsConnections.Hoppie;

public class HoppieDownlinkParserTests
{
    [Theory]
    [InlineData("WILCO", 0, new string[0])]
    [InlineData("REQUEST CLIMB TO FL380", 9, new[] { "FL380" })]
    [InlineData("REQUEST OFFSET 20NM LEFT OF ROUTE", 15, new[] { "20NM", "LEFT" })]
    [InlineData("REQUEST DIRECT TO BIXAD", 22, new[] { "BIXAD" })]
    [InlineData("SQUAWKING 1234", 47, new[] { "1234" })]
    public void Parse_KnownElement_ReturnsElement(string text, int expectedId, string[] expectedParameters)
    {
        // Act
        var elements = HoppieDownlinkParser.Parse(text);

        // Assert
        var element = Assert.Single(elements);
        Assert.Equal(expectedId, element.Id);
        Assert.Equal(expectedParameters, element.Parameters);
    }

    [Theory]
    [InlineData("REQUEST DESCEND TO FL350", 10)] // DESCEND in place of DESCENT
    [InlineData("PRESENT LEVEL FL350", 32)] // LEVEL in place of ALTITUDE
    [InlineData("MAINTAINING FL350", 37)] // Other form of LEVEL [altitude]
    public void Parse_OtherWords_ReturnsElement(string text, int expectedId)
    {
        // Act
        var elements = HoppieDownlinkParser.Parse(text);

        // Assert
        var element = Assert.Single(elements);
        Assert.Equal(expectedId, element.Id);
        Assert.Equal("FL350", Assert.Single(element.Parameters));
    }

    [Fact]
    public void Parse_LowerCaseExtraSpacesAndFullStop_ReturnsElement()
    {
        // Act
        var elements = HoppieDownlinkParser.Parse("request  climb to fl380.");

        // Assert
        var element = Assert.Single(elements);
        Assert.Equal(9, element.Id);
        Assert.Equal("FL380", Assert.Single(element.Parameters));
    }

    [Fact]
    public void Parse_ValueMarkers_AreRemoved()
    {
        // Act
        var elements = HoppieDownlinkParser.Parse("REQUEST CLIMB TO @FL380@");

        // Assert
        var element = Assert.Single(elements);
        Assert.Equal(9, element.Id);
        Assert.Equal("FL380", Assert.Single(element.Parameters));
    }

    [Fact]
    public void Parse_MoreThanOneElement_SplitsTheText()
    {
        // Act
        var elements = HoppieDownlinkParser.Parse("REQUEST CLIMB TO FL380 DUE TO WEATHER");

        // Assert - DM9 REQUEST CLIMB TO [altitude] and DM65 DUE TO WEATHER
        Assert.Equal(2, elements.Count);
        Assert.Equal(9, elements[0].Id);
        Assert.Equal("FL380", Assert.Single(elements[0].Parameters));
        Assert.Equal(65, elements[1].Id);
        Assert.Empty(elements[1].Parameters);
    }

    [Fact]
    public void Parse_StricterParameterType_IsMoreSpecific()
    {
        // "1200" is a time and a position, but a time is stricter,
        // so DM13 AT [time] is used, and not DM11 AT [position]

        // Act
        var elements = HoppieDownlinkParser.Parse("AT 1200 REQUEST CLIMB TO FL380");

        // Assert
        var element = Assert.Single(elements);
        Assert.Equal(13, element.Id);
        Assert.Equal(new[] { "1200", "FL380" }, element.Parameters);
    }

    [Fact]
    public void Parse_FreeTextParameter_GetsTheRemainingText()
    {
        // Act
        var elements = HoppieDownlinkParser.Parse("REQUEST WEATHER DEVIATION TO BIXAD VIA ISTEM MISLY");

        // Assert - DM26 REQUEST WEATHER DEVIATION TO [position] VIA [route clearance]
        var element = Assert.Single(elements);
        Assert.Equal(26, element.Id);
        Assert.Equal(new[] { "BIXAD", "ISTEM MISLY" }, element.Parameters);
    }

    [Theory]
    [InlineData("REQUEST 350")] // DM6 REQUEST [altitude] and DM18 REQUEST [speed]
    [InlineData("REQUEST CLIMB DUE TO A/C PERFORMANCE")] // DM23 REQUEST [procedure name] and DM24 REQUEST [route clearance]
    public void Parse_AmbiguousText_ReturnsFreeText(string text)
    {
        // Act
        var elements = HoppieDownlinkParser.Parse(text);

        // Assert
        var element = Assert.Single(elements);
        Assert.Equal(67, element.Id);
        Assert.Equal(text, Assert.Single(element.Parameters));
    }

    [Fact]
    public void Parse_UnknownText_ReturnsFreeTextWithTheOriginalText()
    {
        // Act
        var elements = HoppieDownlinkParser.Parse("Thank you, good day");

        // Assert - the text is not changed to upper case
        var element = Assert.Single(elements);
        Assert.Equal(67, element.Id);
        Assert.Equal("Thank you, good day", Assert.Single(element.Parameters));
    }
}
