using System.Collections;

namespace CPDLCServer.Cpdlc.Dialogues;

public interface IDialogueRepository
{
    Task Add(Dialogue dialogue, CancellationToken cancellationToken);

    Task<Dialogue?> FindOpenDialogueByUplink(
        string aircraftCallsign,
        string stationId,
        int uplinkMessageId,
        CancellationToken cancellationToken);

    Task<Dialogue?> FindById(Guid id, CancellationToken cancellationToken);

    Task<Dialogue[]> FindOpen(CancellationToken cancellationToken);

    Task<Dialogue[]> FindClosedAndNotArchived(CancellationToken cancellationToken);

    // The open dialogues of an aircraft with an ATSU
    Task<Dialogue[]> FindOpenForAircraft(string aircraftCallsign, string stationId, CancellationToken cancellationToken);

    // Removes the dialogues that opened before the cutoff, and returns the number of dialogues that were removed
    Task<int> RemoveOpenedBefore(DateTimeOffset cutoff, CancellationToken cancellationToken);

    Task<Dialogue[]> All(CancellationToken cancellationToken);

    Task Remove(Dialogue dialogue, CancellationToken cancellationToken);
}
