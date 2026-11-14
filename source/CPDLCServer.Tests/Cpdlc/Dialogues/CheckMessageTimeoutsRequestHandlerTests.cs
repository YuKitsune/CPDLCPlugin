using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Cpdlc.Dialogues;

public class CheckMessageTimeoutsRequestHandlerTests
{
    [Fact]
    public async Task Handle_MarksPilotLate_WhenUplinkTimesOut()
    {
        // Arrange
        var clock = new TestClock();
        var repository = new TestDialogueRepository();
        var publisher = new TestPublisher();

        var startTime = DateTimeOffset.UtcNow;
        clock.SetUtcNow(startTime);

        // Create an uplink that requires a response
        var dialogue = new Dialogue("UAL123");
        var uplink = dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLIMB TO FL410",
            startTime);
        await repository.Add(dialogue, CancellationToken.None);

        var handler = new CheckMessageTimeoutsRequestHandler(repository, clock, publisher, Logger.None);

        // Act - advance time beyond pilot late timeout (2 minutes)
        clock.SetUtcNow(startTime.AddMinutes(3));
        await handler.Handle(new CheckMessageTimeoutsRequest(), CancellationToken.None);

        // Assert
        Assert.True(uplink.IsPilotLate);
        Assert.Single(publisher.PublishedNotifications);
        var notification = Assert.IsType<DialogueChangedNotification>(publisher.PublishedNotifications[0]);
        Assert.Equal(dialogue, notification.Dialogue);
    }

    [Fact]
    public async Task Handle_DoesNotMarkPilotLate_WhenTimeoutNotReached()
    {
        // Arrange
        var clock = new TestClock();
        var repository = new TestDialogueRepository();
        var publisher = new TestPublisher();

        var startTime = DateTimeOffset.UtcNow;
        clock.SetUtcNow(startTime);

        var dialogue = new Dialogue("UAL123");
        var uplink = dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLIMB TO FL410",
            startTime);
        await repository.Add(dialogue, CancellationToken.None);

        var handler = new CheckMessageTimeoutsRequestHandler(repository, clock, publisher, Logger.None);

        // Act - advance time but not beyond timeout (1 minute < 2 minute timeout)
        clock.SetUtcNow(startTime.AddMinutes(1));
        await handler.Handle(new CheckMessageTimeoutsRequest(), CancellationToken.None);

        // Assert
        Assert.False(uplink.IsPilotLate);
        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task Handle_IgnoresNoResponseUplinks()
    {
        // Arrange
        var clock = new TestClock();
        var repository = new TestDialogueRepository();
        var publisher = new TestPublisher();

        var startTime = DateTimeOffset.UtcNow;
        clock.SetUtcNow(startTime);

        // Create an uplink that doesn't require a response
        var dialogue = new Dialogue("UAL123");
        var uplink = dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "ROGER",
            startTime);
        await repository.Add(dialogue, CancellationToken.None);

        var handler = new CheckMessageTimeoutsRequestHandler(repository, clock, publisher, Logger.None);

        // Act - advance time beyond timeout
        clock.SetUtcNow(startTime.AddMinutes(3));
        await handler.Handle(new CheckMessageTimeoutsRequest(), CancellationToken.None);

        // Assert - should not be marked late since no response is needed
        Assert.False(uplink.IsPilotLate);
        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task Handle_MarksControllerLate_WhenDownlinkTimesOut()
    {
        // Arrange
        var clock = new TestClock();
        var repository = new TestDialogueRepository();
        var publisher = new TestPublisher();

        var startTime = DateTimeOffset.UtcNow;
        clock.SetUtcNow(startTime);

        // Create a downlink that requires a response
        var dialogue = new Dialogue("UAL123");
        var downlink = dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            startTime);
        await repository.Add(dialogue, CancellationToken.None);

        var handler = new CheckMessageTimeoutsRequestHandler(repository, clock, publisher, Logger.None);

        // Act - advance time beyond controller late timeout (2 minutes)
        clock.SetUtcNow(startTime.AddMinutes(3));
        await handler.Handle(new CheckMessageTimeoutsRequest(), CancellationToken.None);

        // Assert
        Assert.True(downlink.IsControllerLate);
        Assert.Single(publisher.PublishedNotifications);
        var notification = Assert.IsType<DialogueChangedNotification>(publisher.PublishedNotifications[0]);
        Assert.Equal(dialogue, notification.Dialogue);
    }

    [Fact]
    public async Task Handle_IgnoresClosedMessages()
    {
        // Arrange
        var clock = new TestClock();
        var repository = new TestDialogueRepository();
        var publisher = new TestPublisher();

        var startTime = DateTimeOffset.UtcNow;
        clock.SetUtcNow(startTime);

        var dialogue = new Dialogue("UAL123");
        var uplink = dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLIMB TO FL410",
            startTime);

        // Close the message
        uplink.Close(startTime);

        await repository.Add(dialogue, CancellationToken.None);

        var handler = new CheckMessageTimeoutsRequestHandler(repository, clock, publisher, Logger.None);

        // Act - advance time beyond timeout
        clock.SetUtcNow(startTime.AddMinutes(3));
        await handler.Handle(new CheckMessageTimeoutsRequest(), CancellationToken.None);

        // Assert - closed messages should not be marked late
        Assert.False(uplink.IsPilotLate);
        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task Handle_IgnoresClosedDialogues()
    {
        // Arrange
        var clock = new TestClock();
        var repository = new TestDialogueRepository();
        var publisher = new TestPublisher();

        var startTime = DateTimeOffset.UtcNow;
        clock.SetUtcNow(startTime);

        // Create a self-closing uplink (NoResponse)
        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "ROGER",
            startTime);
        await repository.Add(dialogue, CancellationToken.None);

        var handler = new CheckMessageTimeoutsRequestHandler(repository, clock, publisher, Logger.None);

        // Act - advance time beyond timeout
        clock.SetUtcNow(startTime.AddMinutes(3));
        await handler.Handle(new CheckMessageTimeoutsRequest(), CancellationToken.None);

        // Assert - closed dialogues should be skipped
        Assert.True(dialogue.IsClosed);
        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task Handle_HandlesMultipleDialogues()
    {
        // Arrange
        var clock = new TestClock();
        var repository = new TestDialogueRepository();
        var publisher = new TestPublisher();

        var startTime = DateTimeOffset.UtcNow;
        clock.SetUtcNow(startTime);

        // Create multiple dialogues with different aircraft
        var dialogue1 = new Dialogue("UAL123");
        var uplink1 = dialogue1.AddUplink(1, null, "UAL123", "BN-TSN_FSS", CpdlcUplinkResponseType.WilcoUnable, AlertType.None, "CLIMB", startTime);

        var dialogue2 = new Dialogue("DAL456");
        var uplink2 = dialogue2.AddUplink(2, null, "DAL456", "BN-TSN_FSS", CpdlcUplinkResponseType.WilcoUnable, AlertType.None, "DESCEND", startTime);

        await repository.Add(dialogue1, CancellationToken.None);
        await repository.Add(dialogue2, CancellationToken.None);

        var handler = new CheckMessageTimeoutsRequestHandler(repository, clock, publisher, Logger.None);

        // Act - advance time beyond timeout
        clock.SetUtcNow(startTime.AddMinutes(3));
        await handler.Handle(new CheckMessageTimeoutsRequest(), CancellationToken.None);

        // Assert - both should be marked late
        Assert.True(uplink1.IsPilotLate);
        Assert.True(uplink2.IsPilotLate);
        Assert.Equal(2, publisher.PublishedNotifications.Count);
    }
}
