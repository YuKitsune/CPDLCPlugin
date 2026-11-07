using System.Collections;

namespace CPDLCServer.Cpdlc.Dialogues;

public interface IDialogueRepository
{
    Task Add(Dialogue dialogue, CancellationToken cancellationToken);

    Task<Dialogue?> FindOpenDialogueByUplink(
        string aircraftCallsign,
        int uplinkMessageId,
        CancellationToken cancellationToken);

    Task<Dialogue?> FindById(Guid id, CancellationToken cancellationToken);

    Task<Dialogue[]> FindOpen(CancellationToken cancellationToken);

    Task<Dialogue[]> FindClosedAndNotArchived(CancellationToken cancellationToken);

    Task<Dialogue[]> FindOpenedBefore(DateTimeOffset cutoff, CancellationToken cancellationToken);

    Task<Dialogue[]> All(CancellationToken cancellationToken);

    Task Remove(Dialogue dialogue, CancellationToken cancellationToken);
}
