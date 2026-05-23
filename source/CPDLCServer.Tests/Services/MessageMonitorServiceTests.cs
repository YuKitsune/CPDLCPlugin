using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Services;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Services;

public class MessageMonitorServiceTests
{
    [Fact]
    public async Task CheckForTimeouts_MarksPilotLate_WhenUplinkTimesOut()
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

        var service = new MessageMonitorService(repository, clock, publisher, Logger.None);

        // Act - advance time beyond pilot late timeout (2 minutes)
        clock.SetUtcNow(startTime.AddMinutes(3));
        await service.CheckForTimeouts(CancellationToken.None);

        // Assert
        Assert.True(uplink.IsPilotLate);
        Assert.Single(publisher.PublishedNotifications);
        var notification = Assert.IsType<DialogueChangedNotification>(publisher.PublishedNotifications[0]);
        Assert.Equal(dialogue, notification.Dialogue);
    }

    [Fact]
    public async Task CheckForTimeouts_DoesNotMarkPilotLate_WhenTimeoutNotReached()
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

        var service = new MessageMonitorService(repository, clock, publisher, Logger.None);

        // Act - advance time but not beyond timeout (1 minute < 2 minute timeout)
        clock.SetUtcNow(startTime.AddMinutes(1));
        await service.CheckForTimeouts(CancellationToken.None);

        // Assert
        Assert.False(uplink.IsPilotLate);
        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task CheckForTimeouts_IgnoresNoResponseUplinks()
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

        var service = new MessageMonitorService(repository, clock, publisher, Logger.None);

        // Act - advance time beyond timeout
        clock.SetUtcNow(startTime.AddMinutes(3));
        await service.CheckForTimeouts(CancellationToken.None);

        // Assert - should not be marked late since no response is needed
        Assert.False(uplink.IsPilotLate);
        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task CheckForTimeouts_MarksControllerLate_WhenDownlinkTimesOut()
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

        var service = new MessageMonitorService(repository, clock, publisher, Logger.None);

        // Act - advance time beyond controller late timeout (2 minutes)
        clock.SetUtcNow(startTime.AddMinutes(3));
        await service.CheckForTimeouts(CancellationToken.None);

        // Assert
        Assert.True(downlink.IsControllerLate);
        Assert.Single(publisher.PublishedNotifications);
        var notification = Assert.IsType<DialogueChangedNotification>(publisher.PublishedNotifications[0]);
        Assert.Equal(dialogue, notification.Dialogue);
    }

    [Fact]
    public async Task CheckForTimeouts_IgnoresClosedMessages()
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

        var service = new MessageMonitorService(repository, clock, publisher, Logger.None);

        // Act - advance time beyond timeout
        clock.SetUtcNow(startTime.AddMinutes(3));
        await service.CheckForTimeouts(CancellationToken.None);

        // Assert - closed messages should not be marked late
        Assert.False(uplink.IsPilotLate);
        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task CheckForTimeouts_IgnoresClosedDialogues()
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

        var service = new MessageMonitorService(repository, clock, publisher, Logger.None);

        // Act - advance time beyond timeout
        clock.SetUtcNow(startTime.AddMinutes(3));
        await service.CheckForTimeouts(CancellationToken.None);

        // Assert - closed dialogues should be skipped
        Assert.True(dialogue.IsClosed);
        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task ArchiveCompletedDialogues_ArchivesDialogue_WhenAllMessagesAcknowledgedAndDelayPassed()
    {
        // Arrange
        var clock = new TestClock();
        var repository = new TestDialogueRepository();
        var publisher = new TestPublisher();

        var startTime = DateTimeOffset.UtcNow;
        clock.SetUtcNow(startTime);

        // Create a closed dialogue with acknowledged message
        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "ROGER",
            startTime); // NoResponse uplinks are self-closing and auto-acknowledged
        await repository.Add(dialogue, CancellationToken.None);

        var service = new MessageMonitorService(repository, clock, publisher, Logger.None);

        // Act - advance time beyond archive delay (10 seconds)
        clock.SetUtcNow(startTime.AddSeconds(15));
        await service.ArchiveCompletedDialogues(CancellationToken.None);

        // Assert
        Assert.True(dialogue.IsArchived);
        Assert.Single(publisher.PublishedNotifications);
        var notification = Assert.IsType<DialogueChangedNotification>(publisher.PublishedNotifications[0]);
        Assert.Equal(dialogue, notification.Dialogue);
    }

    [Fact]
    public async Task ArchiveCompletedDialogues_DoesNotArchive_WhenDelayNotPassed()
    {
        // Arrange
        var clock = new TestClock();
        var repository = new TestDialogueRepository();
        var publisher = new TestPublisher();

        var startTime = DateTimeOffset.UtcNow;
        clock.SetUtcNow(startTime);

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

        var service = new MessageMonitorService(repository, clock, publisher, Logger.None);

        // Act - advance time but not enough (5 seconds < 10 second delay)
        clock.SetUtcNow(startTime.AddSeconds(5));
        await service.ArchiveCompletedDialogues(CancellationToken.None);

        // Assert
        Assert.False(dialogue.IsArchived);
        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task ArchiveCompletedDialogues_IgnoresOpenDialogues()
    {
        // Arrange
        var clock = new TestClock();
        var repository = new TestDialogueRepository();
        var publisher = new TestPublisher();

        var startTime = DateTimeOffset.UtcNow;
        clock.SetUtcNow(startTime);

        // Create an open dialogue (requires response)
        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLIMB TO FL410",
            startTime);
        await repository.Add(dialogue, CancellationToken.None);

        var service = new MessageMonitorService(repository, clock, publisher, Logger.None);

        // Act - advance time beyond archive delay
        clock.SetUtcNow(startTime.AddSeconds(15));
        await service.ArchiveCompletedDialogues(CancellationToken.None);

        // Assert - open dialogues should not be archived
        Assert.False(dialogue.IsClosed);
        Assert.False(dialogue.IsArchived);
        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task ArchiveCompletedDialogues_IgnoresDialoguesWithoutAcknowledgements()
    {
        // Arrange
        var clock = new TestClock();
        var repository = new TestDialogueRepository();
        var publisher = new TestPublisher();

        var startTime = DateTimeOffset.UtcNow;
        clock.SetUtcNow(startTime);

        // Create a downlink that's closed but not acknowledged
        var dialogue = new Dialogue("UAL123");
        dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse,
            AlertType.None,
            "WILCO",
            startTime);
        await repository.Add(dialogue, CancellationToken.None);

        var service = new MessageMonitorService(repository, clock, publisher, Logger.None);

        // Act - advance time beyond archive delay
        clock.SetUtcNow(startTime.AddSeconds(15));
        await service.ArchiveCompletedDialogues(CancellationToken.None);

        // Assert - unacknowledged dialogues should not be archived
        Assert.True(dialogue.IsClosed);
        Assert.False(dialogue.IsArchived);
        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task CheckForTimeouts_HandlesMultipleDialogues()
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

        var service = new MessageMonitorService(repository, clock, publisher, Logger.None);

        // Act - advance time beyond timeout
        clock.SetUtcNow(startTime.AddMinutes(3));
        await service.CheckForTimeouts(CancellationToken.None);

        // Assert - both should be marked late
        Assert.True(uplink1.IsPilotLate);
        Assert.True(uplink2.IsPilotLate);
        Assert.Equal(2, publisher.PublishedNotifications.Count);
    }
}
