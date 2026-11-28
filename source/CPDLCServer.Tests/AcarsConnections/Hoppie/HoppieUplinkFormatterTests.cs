using CPDLC.Messages;
using CPDLCServer.AcarsConnections.Hoppie;

namespace CPDLCServer.Tests.AcarsConnections.Hoppie;

public class HoppieUplinkFormatterTests
{
    [Fact]
    public void Format_ElementWithoutParameters_ReturnsTemplate()
    {
        // Act
        var text = HoppieUplinkFormatter.Format([new UplinkMessageElement(0, [])]);

        // Assert
        Assert.Equal("UNABLE", text);
    }

    [Fact]
    public void Format_ElementWithParameter_PutsValueBetweenMarkers()
    {
        // Act
        var text = HoppieUplinkFormatter.Format([new UplinkMessageElement(20, ["FL350"])]);

        // Assert
        Assert.Equal("CLIMB TO AND MAINTAIN @FL350@", text);
    }

    [Fact]
    public void Format_ElementWithMoreThanOneParameter_PutsValuesInSequence()
    {
        // Act
        var text = HoppieUplinkFormatter.Format([new UplinkMessageElement(31, ["FL350", "FL370"])]);

        // Assert
        Assert.Equal("CLIMB TO AND MAINTAIN BLOCK @FL350@ TO @FL370@", text);
    }

    [Fact]
    public void Format_ParametersOfDifferentTypes_PutsEachValueBetweenMarkers()
    {
        // Act
        var text = HoppieUplinkFormatter.Format([new UplinkMessageElement(117, ["BRISBANE CENTRE", "128.6"])]);

        // Assert
        Assert.Equal("CONTACT @BRISBANE CENTRE@ @128.6@", text);
    }

    [Fact]
    public void Format_FreeText_PutsTextBetweenMarkers()
    {
        // Act
        var text = HoppieUplinkFormatter.Format([new UplinkMessageElement(169, ["WHEN READY"])]);

        // Assert
        Assert.Equal("@WHEN READY@", text);
    }

    [Fact]
    public void Format_ValueWithMarker_RemovesMarker()
    {
        // Act
        var text = HoppieUplinkFormatter.Format([new UplinkMessageElement(169, ["OPS@EXAMPLE"])]);

        // Assert
        Assert.Equal("@OPSEXAMPLE@", text);
    }

    [Fact]
    public void Format_MoreThanOneElement_JoinsElements()
    {
        // Act
        var text = HoppieUplinkFormatter.Format(
        [
            new UplinkMessageElement(0, []),
            new UplinkMessageElement(166, [])
        ]);

        // Assert
        Assert.Equal("UNABLE. DUE TO TRAFFIC", text);
    }

    [Fact]
    public void Format_EndService_ReturnsLogoff()
    {
        // Act
        var text = HoppieUplinkFormatter.Format([new UplinkMessageElement(161, [])]);

        // Assert
        Assert.Equal("LOGOFF", text);
    }
}
