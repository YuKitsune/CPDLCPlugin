using CPDLCServer.Extensions;
using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

public class InMemoryDialogueRepository : IDialogueRepository
{
    readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly List<Dialogue> _dialogues = new();

    public async Task Add(Dialogue dialogue, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            _dialogues.Add(dialogue);
        }
    }

    public async Task<Dialogue?> FindDialogueForMessage(
        string aircraftCallsign,
        int messageId,
        CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            // BUG: It's possible to have messageId collisions, since the server generates incrementing ids, and some
            //  aircraft have their own incrementing ids ticking along too.
            //  We need to remove this method, and re-factor how messages are correlated to Dialogues.
            //  Idea:
            //      Split into multiple streams:
            //      1. ReplyToDownlink (DialogueId, MessageId)
            //      2. CreateDialogueFromUplink (No params)
            //      3. AddReplyToUplink (UplinkId), Hoppie doesn't track Dialogue IDs, so we need to look through open dialogues for an uplink with that ID.
            //      4. CreateDialogueFromDownlink (DownlinkId)
            return _dialogues
                .FirstOrDefault(d =>
                    d.AircraftCallsign == aircraftCallsign &&
                    d.Messages.Any(m => m.MessageId == messageId));
        }
    }

    public async Task<Dialogue?> FindById(Guid id, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _dialogues.FirstOrDefault(d => d.Id == id);
        }
    }

    public async Task<Dialogue[]> All(CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _dialogues.ToArray();
        }
    }

    public async Task Remove(Dialogue dialogue, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            _dialogues.Remove(dialogue);
        }
    }
}
