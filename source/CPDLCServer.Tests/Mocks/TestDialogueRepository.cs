using CPDLCServer.Model;
using CPDLCServer.Persistence;

namespace CPDLCServer.Tests.Mocks;

public class TestDialogueRepository : IDialogueRepository
{
    private readonly InMemoryDialogueRepository _inner = new();

    public Task Add(Dialogue dialogue, CancellationToken cancellationToken)
    {
        return _inner.Add(dialogue, cancellationToken);
    }

    public Task<Dialogue?> FindOpenDialogueByUplink(
        string aircraftCallsign,
        int uplinkMessageId,
        CancellationToken cancellationToken)
    {
        return _inner.FindOpenDialogueByUplink(
            aircraftCallsign,
            uplinkMessageId,
            cancellationToken);
    }

    public Task<Dialogue?> FindById(Guid id, CancellationToken cancellationToken)
    {
        return _inner.FindById(id, cancellationToken);
    }

    public Task<Dialogue[]> All(CancellationToken cancellationToken)
    {
        return _inner.All(cancellationToken);
    }


    public Task Remove(Dialogue dialogue, CancellationToken cancellationToken)
    {
        return _inner.Remove(dialogue, cancellationToken);
    }
}
