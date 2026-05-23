using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class AcknowledgeDownlinkCommandHandlerTests
{
    [Fact]
    public async Task Handle_AcknowledgesDownlinkMessage()
    {
        // Arrange
        var dialogueRepository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

        var dialogue = new Dialogue("UAL123");
        var downlink = dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST DESCENT FL350",
            clock.UtcNow());
        await dialogueRepository.Add(dialogue, CancellationToken.None);

        var handler = new AcknowledgeDownlinkCommandHandler(
            dialogueRepository,
            clock,
            publisher,
            Logger.None);

        var command = new AcknowledgeDownlinkCommand(dialogue.Id, 1);

        // Assert - downlink is not acknowledged before
        Assert.False(downlink.IsAcknowledged);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - downlink is acknowledged
        Assert.True(downlink.IsAcknowledged);
        Assert.NotNull(downlink.Acknowledged);
    }

    [Fact]
    public async Task Handle_PublishesDialogueChangedNotification()
    {
        // Arrange
        var dialogueRepository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

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

        var handler = new AcknowledgeDownlinkCommandHandler(
            dialogueRepository,
            clock,
            publisher,
            Logger.None);

        var command = new AcknowledgeDownlinkCommand(dialogue.Id, 1);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - DialogueChangedNotification is published
        Assert.Single(publisher.PublishedNotifications.OfType<DialogueChangedNotification>());
        var notification = publisher.PublishedNotifications.OfType<DialogueChangedNotification>().First();
        Assert.Equal(dialogue.Id, notification.Dialogue.Id);
    }

    [Fact]
    public async Task Handle_DoesNotCloseDialogueWhenMessagesAreStillOpen()
    {
        // Arrange
        var dialogueRepository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

        // Create a downlink that requires a response
        var dialogue = new Dialogue("UAL123");
        var downlink = dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST DESCENT FL350",
            clock.UtcNow());
        await dialogueRepository.Add(dialogue, CancellationToken.None);

        var handler = new AcknowledgeDownlinkCommandHandler(
            dialogueRepository,
            clock,
            publisher,
            Logger.None);

        var command = new AcknowledgeDownlinkCommand(dialogue.Id, 1);

        // Assert - dialogue is open before
        Assert.False(dialogue.IsClosed);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - dialogue is still open because downlink still requires a response (it's acknowledged but not closed)
        Assert.True(downlink.IsAcknowledged);
        Assert.False(downlink.IsClosed); // Still waiting for response
        Assert.False(dialogue.IsClosed);
    }

    [Fact]
    public async Task Handle_ThrowsWhenDialogueNotFound()
    {
        // Arrange
        var dialogueRepository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

        var handler = new AcknowledgeDownlinkCommandHandler(
            dialogueRepository,
            clock,
            publisher,
            Logger.None);

        var nonExistentDialogueId = Guid.NewGuid();
        var command = new AcknowledgeDownlinkCommand(nonExistentDialogueId, 1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsWhenDownlinkMessageNotFoundInDialogue()
    {
        // Arrange
        var dialogueRepository = new TestDialogueRepository();
        var clock = new TestClock();
        var publisher = new TestPublisher();

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

        var handler = new AcknowledgeDownlinkCommandHandler(
            dialogueRepository,
            clock,
            publisher,
            Logger.None);

        // Try to acknowledge a message that doesn't exist in the dialogue
        var command = new AcknowledgeDownlinkCommand(dialogue.Id, 999);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.Handle(command, CancellationToken.None));
    }
}
