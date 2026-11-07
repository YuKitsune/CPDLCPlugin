using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Cpdlc.Dialogues;

// The changes are saved when the unit of work is committed.
// Queries load the rows into the context, then read the local entities, so that the results include entities that are
// added in the unit of work and exclude entities that are removed in it.
public class DialogueRepository(CpdlcDbContext dbContext) : IDialogueRepository
{
    public Task Add(Dialogue dialogue, CancellationToken cancellationToken)
    {
        dbContext.Dialogues.Add(dialogue);
        return Task.CompletedTask;
    }

    public async Task<Dialogue?> FindOpenDialogueByUplink(
        string aircraftCallsign,
        int uplinkMessageId,
        CancellationToken cancellationToken)
    {
        await DialoguesWithMessages()
            .Where(d => d.AircraftCallsign == aircraftCallsign && d.Closed == null)
            .LoadAsync(cancellationToken);

        // BUG: It's possible to have messageId collisions, since the server generates incrementing ids, and some
        //  aircraft have their own incrementing ids ticking along too.
        //  We need to remove this method, and re-factor how messages are correlated to Dialogues.
        //  Idea:
        //      Split into multiple streams:
        //      1. ReplyToDownlink (DialogueId, MessageId)
        //      2. CreateDialogueFromUplink (No params)
        //      3. AddReplyToUplink (UplinkId), Hoppie doesn't track Dialogue IDs, so we need to look through open dialogues for an uplink with that ID.
        //      4. CreateDialogueFromDownlink (DownlinkId)
        return dbContext.Dialogues.Local
            .FirstOrDefault(d =>
                d.AircraftCallsign == aircraftCallsign &&
                !d.IsClosed &&
                d.Messages.OfType<UplinkMessage>().Any(m => m.MessageId == uplinkMessageId));
    }

    public async Task<Dialogue?> FindById(Guid id, CancellationToken cancellationToken)
    {
        await DialoguesWithMessages()
            .Where(d => d.Id == id)
            .LoadAsync(cancellationToken);

        return dbContext.Dialogues.Local.FirstOrDefault(d => d.Id == id);
    }

    public async Task<Dialogue[]> FindOpen(CancellationToken cancellationToken)
    {
        await DialoguesWithMessages()
            .Where(d => d.Closed == null)
            .LoadAsync(cancellationToken);

        return dbContext.Dialogues.Local.Where(d => !d.IsClosed).ToArray();
    }

    public async Task<Dialogue[]> FindClosedAndNotArchived(CancellationToken cancellationToken)
    {
        await DialoguesWithMessages()
            .Where(d => d.Closed != null && d.Archived == null)
            .LoadAsync(cancellationToken);

        return dbContext.Dialogues.Local.Where(d => d.IsClosed && !d.IsArchived).ToArray();
    }

    public async Task<Dialogue[]> FindOpenedBefore(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        await DialoguesWithMessages()
            .Where(d => d.Opened < cutoff)
            .LoadAsync(cancellationToken);

        return dbContext.Dialogues.Local.Where(d => d.Opened < cutoff).ToArray();
    }

    public async Task<Dialogue[]> All(CancellationToken cancellationToken)
    {
        await DialoguesWithMessages().LoadAsync(cancellationToken);
        return dbContext.Dialogues.Local.ToArray();
    }

    public Task Remove(Dialogue dialogue, CancellationToken cancellationToken)
    {
        dbContext.Dialogues.Remove(dialogue);
        return Task.CompletedTask;
    }

    IQueryable<Dialogue> DialoguesWithMessages()
    {
        // Load the messages in their order in the dialogue
        return dbContext.Dialogues
            .Include(d => d.Messages.OrderBy(m => m.Position));
    }
}
