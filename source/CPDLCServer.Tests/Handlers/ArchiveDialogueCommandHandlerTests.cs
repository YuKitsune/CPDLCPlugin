using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class ArchiveDialogueCommandHandlerTests
{
    [Fact]
    public async Task Handle_ArchivesClosedDialogue()
    {
        // Arrange
        var repository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(
            1, null, "UAL123", "BN-TSN_FSS",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None, "ROGER",
            clock.UtcNow());
        await repository.Add(dialogue, CancellationToken.None);

        var handler = new ArchiveDialogueCommandHandler(repository, clock, publisher, Logger.None);
        var command = new ArchiveDialogueCommand(dialogue.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(dialogue.IsArchived);
        Assert.Single(publisher.PublishedNotifications.OfType<DialogueChangedNotification>());
    }

    [Fact]
    public async Task Handle_AcknowledgesUnacknowledgedDownlinks_WhenArchiving()
    {
        // Arrange
        var repository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(
            1, null, "UAL123", "BN-TSN_FSS",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None, "CLIMB TO FL350",
            clock.UtcNow());
        var downlink = dialogue.AddDownlink(
            2, 1, "UAL123",
            CpdlcDownlinkResponseType.NoResponse,
            AlertType.None, "WILCO",
            clock.UtcNow());
        await repository.Add(dialogue, CancellationToken.None);

        Assert.True(dialogue.IsClosed);
        Assert.False(downlink.IsAcknowledged);

        var handler = new ArchiveDialogueCommandHandler(repository, clock, publisher, Logger.None);
        var command = new ArchiveDialogueCommand(dialogue.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(dialogue.IsArchived);
        Assert.True(downlink.IsAcknowledged);
    }

    [Fact]
    public async Task Handle_DoesNotArchive_WhenDialogueIsOpen()
    {
        // Arrange
        var repository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(
            1, null, "UAL123", "BN-TSN_FSS",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None, "CLIMB TO FL350",
            clock.UtcNow());
        await repository.Add(dialogue, CancellationToken.None);

        Assert.False(dialogue.IsClosed);

        var handler = new ArchiveDialogueCommandHandler(repository, clock, publisher, Logger.None);
        var command = new ArchiveDialogueCommand(dialogue.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(dialogue.IsArchived);
        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task Handle_IsIdempotent_WhenAlreadyArchived()
    {
        // Arrange
        var repository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(
            1, null, "UAL123", "BN-TSN_FSS",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None, "ROGER",
            clock.UtcNow());
        dialogue.Archive(clock.UtcNow());
        await repository.Add(dialogue, CancellationToken.None);

        var handler = new ArchiveDialogueCommandHandler(repository, clock, publisher, Logger.None);
        var command = new ArchiveDialogueCommand(dialogue.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - no notification published for already-archived dialogue
        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task Handle_DoesNothing_WhenDialogueNotFound()
    {
        // Arrange
        var repository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

        var handler = new ArchiveDialogueCommandHandler(repository, clock, publisher, Logger.None);
        var command = new ArchiveDialogueCommand(Guid.NewGuid());

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Empty(publisher.PublishedNotifications);
    }
}
