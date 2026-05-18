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

    public async Task<Dialogue?> FindOpenDialogueByUplink(
        string aircraftCallsign,
        int uplinkMessageId,
        CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _dialogues
                .FirstOrDefault(d =>
                    d.AircraftCallsign == aircraftCallsign &&
                    !d.IsArchived &&
                    d.Messages.OfType<UplinkMessage>().Any(m => m.MessageId == uplinkMessageId));
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
