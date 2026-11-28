using CPDLC.Messages;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Uplinks;

namespace CPDLCServer.Tests.Cpdlc.Dialogues;

public class ControlMessagesTests
{
    // The logoff is queued only for an END SERVICE with other elements that need a response
    [Theory]
    [InlineData(new[] { 161 }, false)] // END SERVICE alone
    [InlineData(new[] { 161, 169 }, true)] // END SERVICE with free text (R)
    [InlineData(new[] { 169, 161 }, true)] // Free text (R) with END SERVICE
    [InlineData(new[] { 161, 117 }, true)] // END SERVICE with CONTACT (W/U)
    [InlineData(new[] { 159, 161 }, false)] // ERROR (NE) with END SERVICE
    [InlineData(new[] { 169 }, false)] // No END SERVICE
    public void NeedsLogoffAfterResponse_DependsOnTheOtherElements(int[] elementIds, bool expected)
    {
        // Arrange
        var elements = elementIds.Select(CreateElement).ToArray();
        var uplink = new UplinkMessage(Guid.NewGuid(), 1, null, "UAL123", "BN-TSN_FSS", elements, false, DateTimeOffset.UtcNow);

        // Act
        var result = ControlMessages.NeedsLogoffAfterResponse(uplink);

        // Assert
        Assert.Equal(expected, result);
    }

    static UplinkMessageElement CreateElement(int elementId)
    {
        return elementId switch
        {
            117 => new UplinkMessageElement(elementId, ["MELBOURNE CTR", "123.450"]),
            159 => new UplinkMessageElement(elementId, ["TEST"]),
            169 => new UplinkMessageElement(elementId, ["MONITOR UNICOM"]),
            _ => new UplinkMessageElement(elementId, [])
        };
    }
}
