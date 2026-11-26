using CPDLC.Messages;
using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Tests.Mocks;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Tests.Persistence;

public class CpdlcDbContextTests
{
    static readonly DateTimeOffset Start = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChanges_WhenEntityChangedByDifferentUnitOfWork_Throws()
    {
        // Arrange
        using var database = new TestDatabase();
        var connection = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.NextDataAuthority);
        await using (var dbContext = database.CreateDbContext())
        {
            await new AircraftRepository(dbContext).Add(new("UAL123", "hoppies-ybbb"), connection, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        await using var first = database.CreateDbContext();
        await using var second = database.CreateDbContext();
        var firstConnection = await new AircraftRepository(first).Find(new("UAL123", "hoppies-ybbb"), CancellationToken.None);
        var secondConnection = await new AircraftRepository(second).Find(new("UAL123", "hoppies-ybbb"), CancellationToken.None);

        firstConnection!.PromoteToCurrentDataAuthority();
        secondConnection!.SetNextDataAuthority("YMMM", Start);

        // Act
        await first.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_WhenMessagesOfSameDialogueChangedByDifferentUnitOfWork_Throws()
    {
        // Arrange
        using var database = new TestDatabase();
        var dialogue = new Dialogue("UAL123", "YBBB");
        dialogue.AddDownlink(1, null, "UAL123", CpdlcDownlinkResponseType.ResponseRequired, [new DownlinkMessageElement(9, ["FL410"])], Start);
        dialogue.AddDownlink(2, null, "UAL123", CpdlcDownlinkResponseType.ResponseRequired, [new DownlinkMessageElement(22, ["BIXAD"])], Start);
        await using (var dbContext = database.CreateDbContext())
        {
            await new DialogueRepository(dbContext).Add(dialogue, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        await using var first = database.CreateDbContext();
        await using var second = database.CreateDbContext();
        var firstDialogue = await new DialogueRepository(first).FindById(dialogue.Id, CancellationToken.None);
        var secondDialogue = await new DialogueRepository(second).FindById(dialogue.Id, CancellationToken.None);

        // Each unit of work changes a different message. Only the messages change, not the dialogue.
        firstDialogue!.AcknowledgeDownlink(1, Start.AddMinutes(1));
        secondDialogue!.AcknowledgeDownlink(2, Start.AddMinutes(1));

        // Act
        await first.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_WhenSameEntityAddedByDifferentUnitOfWork_Throws()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var first = database.CreateDbContext();
        await using var second = database.CreateDbContext();

        await new MessageIdProvider(first).GetNextMessageId("hoppies-ybbb", "UAL123", CancellationToken.None);
        await new MessageIdProvider(second).GetNextMessageId("hoppies-ybbb", "UAL123", CancellationToken.None);

        // Act
        await first.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_WhenNoConflict_Saves()
    {
        // Arrange
        using var database = new TestDatabase();
        var dialogue = new Dialogue("UAL123", "YBBB");
        dialogue.AddDownlink(1, null, "UAL123", CpdlcDownlinkResponseType.ResponseRequired, [new DownlinkMessageElement(9, ["FL410"])], Start);
        await using (var dbContext = database.CreateDbContext())
        {
            await new DialogueRepository(dbContext).Add(dialogue, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Act - two units of work, one after the other
        foreach (var minutes in new[] { 1, 2 })
        {
            await using var dbContext = database.CreateDbContext();
            var saved = await new DialogueRepository(dbContext).FindById(dialogue.Id, CancellationToken.None);
            saved!.AddUplink(minutes + 10, null, "UAL123", "BN-TSN_FSS", [new UplinkMessageElement(1, [])], false, Start.AddMinutes(minutes));
            await dbContext.SaveChangesAsync();
        }

        // Assert
        await using var readDbContext = database.CreateDbContext();
        var result = await new DialogueRepository(readDbContext).FindById(dialogue.Id, CancellationToken.None);
        Assert.Equal(3, result!.Messages.Count);
    }
}
