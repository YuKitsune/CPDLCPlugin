using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Cpdlc.Uplinks;

public class MarkUplinkTransmissionFailedCommandHandlerTests
{
    [Fact]
    public async Task Handle_MarksUplinkAsFailedAndPublishesDialogueChanged()
    {
        // Arrange
        var dialogueRepository = new TestDialogueRepository();
        var publisher = new TestPublisher();

        var dialogue = new Dialogue("UAL123", "YBBB");
        var uplink = dialogue.AddUplink(3, null, "UAL123", "BN-TSN_FSS", CpdlcUplinkResponseType.WilcoUnable, AlertType.None, "CLIMB TO FL410", DateTimeOffset.UtcNow);
        await dialogueRepository.Add(dialogue, CancellationToken.None);

        var handler = new MarkUplinkTransmissionFailedCommandHandler(dialogueRepository, publisher, Logger.None);

        // Act
        await handler.Handle(new MarkUplinkTransmissionFailedCommand(dialogue.Id, 3), CancellationToken.None);

        // Assert
        Assert.True(uplink.DidTransmissionFail);
        var notification = Assert.Single(publisher.PublishedNotifications.OfType<DialogueChangedNotification>());
        Assert.Same(dialogue, notification.Dialogue);
    }

    [Fact]
    public async Task Handle_DialogueNotFound_DoesNothing()
    {
        // Arrange
        var publisher = new TestPublisher();
        var handler = new MarkUplinkTransmissionFailedCommandHandler(new TestDialogueRepository(), publisher, Logger.None);

        // Act
        await handler.Handle(new MarkUplinkTransmissionFailedCommand(Guid.NewGuid(), 3), CancellationToken.None);

        // Assert
        Assert.Empty(publisher.PublishedNotifications);
    }
}
