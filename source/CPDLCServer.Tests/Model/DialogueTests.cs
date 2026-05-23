using CPDLCServer.Model;

namespace CPDLCServer.Tests.Model;

public class DialogueTests
{
    [Fact]
    public void Constructor_AddsFirstMessage()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var dialogue = new Dialogue("UAL123");

        // Act
        var downlink = dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        // Assert
        Assert.Single(dialogue.Messages);
        Assert.Equal(downlink, dialogue.Messages[0]);
    }

    [Fact]
    public void Constructor_SetsOpenedTimeToFirstMessageTime()
    {
        // Arrange
        var time = new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var dialogue = new Dialogue("UAL123");

        // Act
        dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        // Assert
        Assert.Equal(time, dialogue.Opened);
    }

    [Fact]
    public void Constructor_SetsCallsignFromParameter()
    {
        // Arrange
        var dialogue = new Dialogue("UAL123");

        // Act
        dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal("UAL123", dialogue.AircraftCallsign);
    }

    [Fact]
    public void AddMessage_UplinkResponseClosesReferencedDownlink()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var dialogue = new Dialogue("UAL123");
        var downlink = dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        // Act
        dialogue.AddUplink(
            2,
            1, // References downlink message 1
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "UNABLE",
            time.AddSeconds(10));

        // Assert
        Assert.True(downlink.IsClosed);
        Assert.True(downlink.IsAcknowledged);
    }

    [Fact]
    public void AddMessage_DownlinkResponseClosesReferencedUplink()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var dialogue = new Dialogue("UAL123");
        var uplink = dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLIMB TO FL410",
            time);

        // Act
        dialogue.AddDownlink(
            2,
            1, // References uplink message 1
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse,
            AlertType.None,
            "WILCO",
            time.AddSeconds(10));

        // Assert
        Assert.True(uplink.IsClosed);
        Assert.True(uplink.IsAcknowledged);
    }

    [Fact]
    public void AddMessage_StandbyDownlinkDoesNotCloseUplink()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var dialogue = new Dialogue("UAL123");
        var uplink = dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLIMB FL410",
            time);

        // Act
        dialogue.AddDownlink(
            2,
            1, // References uplink message 1
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse,
            AlertType.None,
            "STANDBY",
            time.AddSeconds(10));

        // Assert
        Assert.False(uplink.IsClosed);
    }

    [Fact]
    public void AddMessage_StandbyUplinkDoesNotCloseDownlink()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var dialogue = new Dialogue("UAL123");
        var downlink = dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        // Act
        dialogue.AddUplink(
            2,
            1, // References downlink message 1
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "STANDBY",
            time.AddSeconds(10));

        // Assert
        Assert.False(downlink.IsClosed);
        Assert.False(downlink.IsAcknowledged);
    }

    [Fact]
    public void AddMessage_RequestDeferredUplinkDoesNotCloseDownlink()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var dialogue = new Dialogue("UAL123");
        var downlink = dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        // Act
        dialogue.AddUplink(
            2,
            1, // References downlink message 1
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "REQUEST DEFERRED",
            time.AddSeconds(10));

        // Assert
        Assert.False(downlink.IsClosed);
        Assert.False(downlink.IsAcknowledged);
    }

    [Fact]
    public void Dialogue_ClosesWhenAllMessagesAreClosed()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var dialogue = new Dialogue("UAL123");
        dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        var uplink = dialogue.AddUplink(
            2,
            1, // References downlink message 1
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.NoResponse, // No response required, so self-closing
            AlertType.None,
            "UNABLE",
            time.AddSeconds(10));

        // Assert
        Assert.True(dialogue.IsClosed);
        Assert.NotNull(dialogue.Closed);
        Assert.Equal(uplink.Sent, dialogue.Closed);
    }

    [Fact]
    public void Dialogue_RemainsOpenWhenSomeMessagesAreOpen()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var dialogue = new Dialogue("UAL123");
        var downlink = dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        var uplink = dialogue.AddUplink(
            2,
            1, // References downlink message 1
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable, // Requires response, not self-closing
            AlertType.None,
            "CLIMB TO FL410",
            time.AddSeconds(10));

        // Assert - downlink is closed by uplink response, but uplink requires response so stays open
        Assert.True(downlink.IsClosed);
        Assert.False(uplink.IsClosed);
        Assert.False(dialogue.IsClosed);
        Assert.Null(dialogue.Closed);
    }

    [Fact]
    public void Dialogue_MessageRequiringNoResponseIsSelfClosing()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;
        var dialogue = new Dialogue("UAL123");

        // Act
        var downlink = dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse, // No response required
            AlertType.None,
            "POSITION REPORT",
            time);

        // Assert
        Assert.True(downlink.IsClosed);
        Assert.True(dialogue.IsClosed);
        Assert.Equal(time, dialogue.Closed);
    }

    [Fact]
    public void Dialogue_MultipleMessagesAndResponses()
    {
        // Arrange - Simulate a realistic CPDLC exchange
        var time = DateTimeOffset.UtcNow;

        var dialogue = new Dialogue("UAL123");

        // Pilot requests climb
        var downlink1 = dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            time);

        Assert.False(dialogue.IsClosed); // Dialogue open - downlink awaiting response

        // Controller sends STANDBY
        dialogue.AddUplink(
            2,
            1,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "STANDBY",
            time.AddSeconds(5));

        Assert.False(downlink1.IsClosed); // STANDBY doesn't close the request
        Assert.False(dialogue.IsClosed);

        // Instruction issued
        var uplink2 = dialogue.AddUplink(
            3,
            1,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLIMG TO FL410",
            time.AddSeconds(30));

        Assert.True(downlink1.IsClosed); // Now the request is closed
        Assert.True(downlink1.IsAcknowledged);
        Assert.False(dialogue.IsClosed); // But dialogue still open - uplink needs response

        // Pilot acknowledges
        var downlink2 = dialogue.AddDownlink(
            4,
            3,
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse,
            AlertType.None,
            "WILCO",
            time.AddSeconds(40));

        Assert.True(uplink2.IsClosed);
        Assert.True(uplink2.IsAcknowledged);
        Assert.True(dialogue.IsClosed); // All messages closed, dialogue closes
        Assert.Equal(downlink2.Received, dialogue.Closed);
    }

    [Fact]
    public void Dialogue_LogonRequestAndAcceptanceClosesImmediately()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;

        var dialogue = new Dialogue("UAL123");

        // Pilot sends logon request
        var logonRequest = dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST LOGON",
            time);

        Assert.False(dialogue.IsClosed); // Dialogue open - waiting for response

        // System sends LOGON ACCEPTED (NoResponse type, so self-closing)
        var logonAccepted = dialogue.AddUplink(
            2,
            1, // References logon request
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "LOGON ACCEPTED",
            time.AddSeconds(1));

        // Assert
        Assert.True(logonRequest.IsClosed); // Request is closed by the acceptance
        Assert.True(logonRequest.IsAcknowledged); // Request is auto-acknowledged
        Assert.True(logonAccepted.IsClosed); // Acceptance is self-closing (NoResponse)
        Assert.True(dialogue.IsClosed); // Dialogue closes immediately
        Assert.Equal(logonAccepted.Sent, dialogue.Closed);
        Assert.True(dialogue.IsArchived); // Dialogue is auto-archived with LOGON ACCEPTED
        Assert.Equal(logonAccepted.Sent, dialogue.Archived);
    }
}
