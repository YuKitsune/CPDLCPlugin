using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class AcknowledgeUplinkCommandHandlerTests
{
    [Fact]
    public async Task Handle_ClosesUplinkMessageManually()
    {
        // Arrange
        var dialogueRepository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

        var dialogue = new Dialogue("UAL123");
        var uplink = dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLEARED DESCEND FL350",
            clock.UtcNow());
        await dialogueRepository.Add(dialogue, CancellationToken.None);

        var handler = new AcknowledgeUplinkCommandHandler(
            dialogueRepository,
            clock,
            publisher,
            Logger.None);

        var command = new AcknowledgeUplinkCommand(dialogue.Id, 1);

        // Assert - uplink is not closed before
        Assert.False(uplink.IsClosed);
        Assert.False(uplink.ClosedManually);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - uplink is closed manually
        Assert.True(uplink.IsClosed);
        Assert.True(uplink.ClosedManually);
        Assert.NotNull(uplink.Closed);
    }

    [Fact]
    public async Task Handle_PublishesDialogueChangedNotification()
    {
        // Arrange
        var dialogueRepository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLEARED DESCEND FL350",
            clock.UtcNow());
        await dialogueRepository.Add(dialogue, CancellationToken.None);

        var handler = new AcknowledgeUplinkCommandHandler(
            dialogueRepository,
            clock,
            publisher,
            Logger.None);

        var command = new AcknowledgeUplinkCommand(dialogue.Id, 1);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - DialogueChangedNotification is published
        Assert.Single(publisher.PublishedNotifications.OfType<DialogueChangedNotification>());
        var notification = publisher.PublishedNotifications.OfType<DialogueChangedNotification>().First();
        Assert.Equal(dialogue.Id, notification.Dialogue.Id);
    }

    [Fact]
    public async Task Handle_ClosesDialogueWhenAllMessagesAreClosed()
    {
        // Arrange
        var dialogueRepository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

        // Create an uplink that requires a response
        var dialogue = new Dialogue("UAL123");
        var uplink = dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLEARED DESCEND FL350",
            clock.UtcNow());
        await dialogueRepository.Add(dialogue, CancellationToken.None);

        var handler = new AcknowledgeUplinkCommandHandler(
            dialogueRepository,
            clock,
            publisher,
            Logger.None);

        var command = new AcknowledgeUplinkCommand(dialogue.Id, 1);

        // Assert - dialogue is open before
        Assert.False(dialogue.IsClosed);

        // Act - manually close the uplink
        await handler.Handle(command, CancellationToken.None);

        // Assert - dialogue is now closed because all messages are closed
        Assert.True(uplink.IsClosed);
        Assert.True(uplink.ClosedManually);
        Assert.True(dialogue.IsClosed);
    }

    [Fact]
    public async Task Handle_KeepsDialogueOpenWhenOtherMessagesAreStillOpen()
    {
        // Arrange
        var dialogueRepository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

        var dialogue = new Dialogue("UAL123");
        var uplink1 = dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLEARED DESCEND FL350",
            clock.UtcNow());

        var uplink2 = dialogue.AddUplink(
            2,
            null,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLEARED DIRECT WAYPOINT",
            clock.UtcNow());

        await dialogueRepository.Add(dialogue, CancellationToken.None);

        var handler = new AcknowledgeUplinkCommandHandler(
            dialogueRepository,
            clock,
            publisher,
            Logger.None);

        var command = new AcknowledgeUplinkCommand(dialogue.Id, 1);

        // Assert - dialogue and both uplinks are open before
        Assert.False(dialogue.IsClosed);
        Assert.False(uplink1.IsClosed);
        Assert.False(uplink2.IsClosed);

        // Act - manually close the first uplink
        await handler.Handle(command, CancellationToken.None);

        // Assert - first uplink is closed, but dialogue remains open because uplink2 is still open
        Assert.True(uplink1.IsClosed);
        Assert.True(uplink1.ClosedManually);
        Assert.False(uplink2.IsClosed);
        Assert.False(dialogue.IsClosed);
    }

    [Fact]
    public async Task Handle_ThrowsWhenDialogueNotFound()
    {
        // Arrange
        var dialogueRepository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

        var handler = new AcknowledgeUplinkCommandHandler(
            dialogueRepository,
            clock,
            publisher,
            Logger.None);

        var nonExistentDialogueId = Guid.NewGuid();
        var command = new AcknowledgeUplinkCommand(nonExistentDialogueId, 1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsWhenUplinkMessageNotFoundInDialogue()
    {
        // Arrange
        var dialogueRepository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLEARED DESCEND FL350",
            clock.UtcNow());
        await dialogueRepository.Add(dialogue, CancellationToken.None);

        var handler = new AcknowledgeUplinkCommandHandler(
            dialogueRepository,
            clock,
            publisher,
            Logger.None);

        // Try to acknowledge a message that doesn't exist in the dialogue
        var command = new AcknowledgeUplinkCommand(dialogue.Id, 999);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OnlyFindsUplinkMessages()
    {
        // Arrange
        var dialogueRepository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

        // Create a dialogue with a downlink (not an uplink)
        var dialogue = new Dialogue("UAL123");
        dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST DESCENT FL350",
            clock.UtcNow());
        await dialogueRepository.Add(dialogue, CancellationToken.None);

        var handler = new AcknowledgeUplinkCommandHandler(
            dialogueRepository,
            clock,
            publisher,
            Logger.None);

        // Try to acknowledge message ID 1 (which is a downlink, not an uplink)
        var command = new AcknowledgeUplinkCommand(dialogue.Id, 1);

        // Act & Assert - should throw because it can't find an uplink with that ID
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.Handle(command, CancellationToken.None));
    }
}
