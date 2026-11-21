using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Settings;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Cpdlc.Dialogues;

public class PurgeExpiredCpdlcDataRequestHandlerTests
{
    [Fact]
    public async Task Handle_RemovesDialoguesOpenedMoreThan24HoursAgo()
    {
        // Arrange
        var clock = new TestClock();
        var dialogueRepository = new TestDialogueRepository();
        var settingsProvider = new TestServerSettingsProvider(new ServerSettings { DialogueRetentionHours = 24 });

        var startTime = DateTimeOffset.UtcNow;
        clock.SetUtcNow(startTime);

        var oldDialogue = new Dialogue("UAL123", "YBBB");
        oldDialogue.AddUplink(1, null, "UAL123", "BN-TSN_FSS", CpdlcUplinkResponseType.WilcoUnable, AlertType.None, "CLIMB TO FL410", startTime);
        var newDialogue = new Dialogue("QFA1", "YBBB");
        newDialogue.AddUplink(1, null, "QFA1", "BN-TSN_FSS", CpdlcUplinkResponseType.WilcoUnable, AlertType.None, "CLIMB TO FL410", startTime.AddHours(2));
        await dialogueRepository.Add(oldDialogue, CancellationToken.None);
        await dialogueRepository.Add(newDialogue, CancellationToken.None);

        var handler = new PurgeExpiredCpdlcDataRequestHandler(dialogueRepository, settingsProvider, clock, Logger.None);

        // Act - advance time beyond the purge timeout (24 hours) for the old dialogue only
        clock.SetUtcNow(startTime.AddHours(25));
        await handler.Handle(new PurgeExpiredCpdlcDataRequest(), CancellationToken.None);

        // Assert
        Assert.Equal([newDialogue], await dialogueRepository.All(CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UsesDialogueRetentionSetting()
    {
        // Arrange
        var clock = new TestClock();
        var dialogueRepository = new TestDialogueRepository();
        var settingsProvider = new TestServerSettingsProvider(new ServerSettings { DialogueRetentionHours = 2 });

        var startTime = DateTimeOffset.UtcNow;
        clock.SetUtcNow(startTime);

        var dialogue = new Dialogue("UAL123", "YBBB");
        dialogue.AddUplink(1, null, "UAL123", "BN-TSN_FSS", CpdlcUplinkResponseType.WilcoUnable, AlertType.None, "CLIMB TO FL410", startTime);
        await dialogueRepository.Add(dialogue, CancellationToken.None);

        var handler = new PurgeExpiredCpdlcDataRequestHandler(dialogueRepository, settingsProvider, clock, Logger.None);

        // Act - advance time beyond the configured retention (2 hours)
        clock.SetUtcNow(startTime.AddHours(3));
        await handler.Handle(new PurgeExpiredCpdlcDataRequest(), CancellationToken.None);

        // Assert
        Assert.Empty(await dialogueRepository.All(CancellationToken.None));
    }
}
