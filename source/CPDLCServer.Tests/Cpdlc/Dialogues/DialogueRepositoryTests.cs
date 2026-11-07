using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Tests.Mocks;

namespace CPDLCServer.Tests.Cpdlc.Dialogues;

public class DialogueRepositoryTests
{
    static readonly DateTimeOffset Start = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task FindById_ReturnsSavedDialogueWithMessagesInOrder()
    {
        // Arrange
        using var database = new TestDatabase();
        var dialogue = new Dialogue("UAL123");
        dialogue.AddDownlink(7, null, "UAL123", CpdlcDownlinkResponseType.ResponseRequired, AlertType.None, "REQUEST DESCENT FL350", Start);
        dialogue.AddUplink(1, 7, "UAL123", "BN-TSN_FSS", CpdlcUplinkResponseType.WilcoUnable, AlertType.None, "DESCEND TO FL350", Start.AddMinutes(1));
        dialogue.AddDownlink(8, 1, "UAL123", CpdlcDownlinkResponseType.NoResponse, AlertType.None, "WILCO", Start.AddMinutes(2));

        await using (var dbContext = database.CreateDbContext())
        {
            await new DialogueRepository(dbContext).Add(dialogue, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Act
        await using var readDbContext = database.CreateDbContext();
        var saved = await new DialogueRepository(readDbContext).FindById(dialogue.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(saved);
        Assert.Equal("UAL123", saved.AircraftCallsign);
        Assert.Equal(dialogue.Opened, saved.Opened);
        Assert.Equal(dialogue.Closed, saved.Closed);
        Assert.True(saved.IsClosed);
        Assert.Equal([7, 1, 8], saved.Messages.Select(m => m.MessageId));

        var request = Assert.IsType<DownlinkMessage>(saved.Messages[0]);
        Assert.Equal("REQUEST DESCENT FL350", request.Content);
        Assert.Equal(CpdlcDownlinkResponseType.ResponseRequired, request.ResponseType);
        Assert.True(request.IsAcknowledged);

        var clearance = Assert.IsType<UplinkMessage>(saved.Messages[1]);
        Assert.Equal("DESCEND TO FL350", clearance.Content);
        Assert.Equal("BN-TSN_FSS", clearance.SenderCallsign);
        Assert.Equal(CpdlcUplinkResponseType.WilcoUnable, clearance.ResponseType);
        Assert.Equal(7, clearance.MessageReference);
        Assert.Equal(Start.AddMinutes(2), clearance.Closed);
    }

    [Fact]
    public async Task Add_MessageToSavedDialogue_SavesMessage()
    {
        // Arrange
        using var database = new TestDatabase();
        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(1, null, "UAL123", "BN-TSN_FSS", CpdlcUplinkResponseType.WilcoUnable, AlertType.None, "CLIMB TO FL410", Start);

        await using (var dbContext = database.CreateDbContext())
        {
            await new DialogueRepository(dbContext).Add(dialogue, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Act
        await using (var dbContext = database.CreateDbContext())
        {
            var saved = await new DialogueRepository(dbContext).FindById(dialogue.Id, CancellationToken.None);
            saved!.AddDownlink(5, 1, "UAL123", CpdlcDownlinkResponseType.NoResponse, AlertType.None, "WILCO", Start.AddMinutes(1));
            await dbContext.SaveChangesAsync();
        }

        // Assert
        await using var readDbContext = database.CreateDbContext();
        var result = await new DialogueRepository(readDbContext).FindById(dialogue.Id, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(2, result.Messages.Count);
        Assert.True(result.IsClosed);
    }

    [Fact]
    public async Task FindOpenDialogueByUplink_FindsDialogueAddedInSameUnitOfWork()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var dbContext = database.CreateDbContext();
        var repository = new DialogueRepository(dbContext);

        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(1, null, "UAL123", "BN-TSN_FSS", CpdlcUplinkResponseType.WilcoUnable, AlertType.None, "CLIMB TO FL410", Start);
        await repository.Add(dialogue, CancellationToken.None);

        // Act
        var result = await repository.FindOpenDialogueByUplink("UAL123", 1, CancellationToken.None);

        // Assert
        Assert.Same(dialogue, result);
    }

    [Fact]
    public async Task FindOpen_ReturnsOnlyOpenDialogues()
    {
        // Arrange
        using var database = new TestDatabase();
        var open = CreateOpenDialogue("UAL123", Start);
        var closed = CreateClosedDialogue("QFA1", Start);
        await AddAndSave(database, open, closed);

        // Act
        await using var dbContext = database.CreateDbContext();
        var result = await new DialogueRepository(dbContext).FindOpen(CancellationToken.None);

        // Assert
        Assert.Equal([open.Id], result.Select(d => d.Id));
    }

    [Fact]
    public async Task FindClosedAndNotArchived_ReturnsOnlyClosedDialoguesThatAreNotArchived()
    {
        // Arrange
        using var database = new TestDatabase();
        var open = CreateOpenDialogue("UAL123", Start);
        var closed = CreateClosedDialogue("QFA1", Start);
        var archived = CreateClosedDialogue("VOZ1", Start);
        archived.Archive(Start.AddMinutes(1));
        await AddAndSave(database, open, closed, archived);

        // Act
        await using var dbContext = database.CreateDbContext();
        var result = await new DialogueRepository(dbContext).FindClosedAndNotArchived(CancellationToken.None);

        // Assert
        Assert.Equal([closed.Id], result.Select(d => d.Id));
    }

    [Fact]
    public async Task FindOpenedBefore_ReturnsOnlyOlderDialogues()
    {
        // Arrange
        using var database = new TestDatabase();
        var older = CreateOpenDialogue("UAL123", Start);
        var newer = CreateOpenDialogue("QFA1", Start.AddHours(2));
        await AddAndSave(database, older, newer);

        // Act
        await using var dbContext = database.CreateDbContext();
        var result = await new DialogueRepository(dbContext).FindOpenedBefore(Start.AddHours(1), CancellationToken.None);

        // Assert
        Assert.Equal([older.Id], result.Select(d => d.Id));
    }

    [Fact]
    public async Task Remove_DeletesDialogueAndMessages()
    {
        // Arrange
        using var database = new TestDatabase();
        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(1, null, "UAL123", "BN-TSN_FSS", CpdlcUplinkResponseType.WilcoUnable, AlertType.None, "CLIMB TO FL410", Start);

        await using (var dbContext = database.CreateDbContext())
        {
            await new DialogueRepository(dbContext).Add(dialogue, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Act
        await using (var dbContext = database.CreateDbContext())
        {
            var repository = new DialogueRepository(dbContext);
            var saved = await repository.FindById(dialogue.Id, CancellationToken.None);
            await repository.Remove(saved!, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        await using var readDbContext = database.CreateDbContext();
        Assert.Empty(await new DialogueRepository(readDbContext).All(CancellationToken.None));
        Assert.Empty(readDbContext.Set<CpdlcMessage>());
    }

    static async Task AddAndSave(TestDatabase database, params Dialogue[] dialogues)
    {
        await using var dbContext = database.CreateDbContext();
        var repository = new DialogueRepository(dbContext);

        foreach (var dialogue in dialogues)
            await repository.Add(dialogue, CancellationToken.None);

        await dbContext.SaveChangesAsync();
    }

    static Dialogue CreateOpenDialogue(string callsign, DateTimeOffset time)
    {
        var dialogue = new Dialogue(callsign);
        dialogue.AddUplink(1, null, callsign, "BN-TSN_FSS", CpdlcUplinkResponseType.WilcoUnable, AlertType.None, "CLIMB TO FL410", time);
        return dialogue;
    }

    static Dialogue CreateClosedDialogue(string callsign, DateTimeOffset time)
    {
        var dialogue = new Dialogue(callsign);
        dialogue.AddUplink(1, null, callsign, "BN-TSN_FSS", CpdlcUplinkResponseType.NoResponse, AlertType.None, "MONITOR BRISBANE CENTRE 128.6", time);
        return dialogue;
    }
}
