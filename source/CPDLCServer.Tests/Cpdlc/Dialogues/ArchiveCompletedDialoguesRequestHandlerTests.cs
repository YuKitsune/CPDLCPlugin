using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Cpdlc.Dialogues;

public class ArchiveCompletedDialoguesRequestHandlerTests
{
    [Fact]
    public async Task Handle_ArchivesDialogue_WhenAllMessagesAcknowledgedAndDelayPassed()
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

        var handler = new ArchiveCompletedDialoguesRequestHandler(repository, clock, publisher, Logger.None);

        // Act - advance time beyond archive delay (10 seconds)
        clock.SetUtcNow(startTime.AddSeconds(15));
        await handler.Handle(new ArchiveCompletedDialoguesRequest(), CancellationToken.None);

        // Assert
        Assert.True(dialogue.IsArchived);
        Assert.Single(publisher.PublishedNotifications);
        var notification = Assert.IsType<DialogueChangedNotification>(publisher.PublishedNotifications[0]);
        Assert.Equal(dialogue, notification.Dialogue);
    }

    [Fact]
    public async Task Handle_DoesNotArchive_WhenDelayNotPassed()
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

        var handler = new ArchiveCompletedDialoguesRequestHandler(repository, clock, publisher, Logger.None);

        // Act - advance time but not enough (5 seconds < 10 second delay)
        clock.SetUtcNow(startTime.AddSeconds(5));
        await handler.Handle(new ArchiveCompletedDialoguesRequest(), CancellationToken.None);

        // Assert
        Assert.False(dialogue.IsArchived);
        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task Handle_IgnoresOpenDialogues()
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

        var handler = new ArchiveCompletedDialoguesRequestHandler(repository, clock, publisher, Logger.None);

        // Act - advance time beyond archive delay
        clock.SetUtcNow(startTime.AddSeconds(15));
        await handler.Handle(new ArchiveCompletedDialoguesRequest(), CancellationToken.None);

        // Assert - open dialogues should not be archived
        Assert.False(dialogue.IsClosed);
        Assert.False(dialogue.IsArchived);
        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task Handle_IgnoresDialoguesWithoutAcknowledgements()
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

        var handler = new ArchiveCompletedDialoguesRequestHandler(repository, clock, publisher, Logger.None);

        // Act - advance time beyond archive delay
        clock.SetUtcNow(startTime.AddSeconds(15));
        await handler.Handle(new ArchiveCompletedDialoguesRequest(), CancellationToken.None);

        // Assert - unacknowledged dialogues should not be archived
        Assert.True(dialogue.IsClosed);
        Assert.False(dialogue.IsArchived);
        Assert.Empty(publisher.PublishedNotifications);
    }
}
