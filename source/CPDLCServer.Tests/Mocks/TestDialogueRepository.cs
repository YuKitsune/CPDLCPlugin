using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Uplinks;

namespace CPDLCServer.Tests.Mocks;

public class TestDialogueRepository : IDialogueRepository
{
    private readonly List<Dialogue> _dialogues = [];

    public Task Add(Dialogue dialogue, CancellationToken cancellationToken)
    {
        _dialogues.Add(dialogue);
        return Task.CompletedTask;
    }

    public Task<Dialogue?> FindOpenDialogueByUplink(
        string aircraftCallsign,
        int uplinkMessageId,
        CancellationToken cancellationToken)
    {
        var dialogue = _dialogues.FirstOrDefault(d =>
            d.AircraftCallsign == aircraftCallsign &&
            !d.IsClosed &&
            d.Messages.OfType<UplinkMessage>().Any(m => m.MessageId == uplinkMessageId));

        return Task.FromResult(dialogue);
    }

    public Task<Dialogue?> FindById(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_dialogues.FirstOrDefault(d => d.Id == id));
    }

    public Task<Dialogue[]> FindOpen(CancellationToken cancellationToken)
    {
        return Task.FromResult(_dialogues.Where(d => !d.IsClosed).ToArray());
    }

    public Task<Dialogue[]> FindClosedAndNotArchived(CancellationToken cancellationToken)
    {
        return Task.FromResult(_dialogues.Where(d => d.IsClosed && !d.IsArchived).ToArray());
    }

    public Task<Dialogue[]> FindOpenedBefore(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        return Task.FromResult(_dialogues.Where(d => d.Opened < cutoff).ToArray());
    }

    public Task<Dialogue[]> All(CancellationToken cancellationToken)
    {
        return Task.FromResult(_dialogues.ToArray());
    }

    public Task Remove(Dialogue dialogue, CancellationToken cancellationToken)
    {
        _dialogues.Remove(dialogue);
        return Task.CompletedTask;
    }
}
