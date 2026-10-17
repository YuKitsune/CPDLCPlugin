using CPDLCServer.Extensions;
using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

public class InMemoryVatsimPilotRepository : IVatsimPilotRepository
{
    readonly SemaphoreSlim _semaphore = new(1, 1);

    private Dictionary<string, VatsimPilot> _pilots = new();

    public async Task Replace(VatsimPilot[] pilots, CancellationToken cancellationToken)
    {
        // A callsign can occur more than once in the data, so keep the last pilot
        var newPilots = new Dictionary<string, VatsimPilot>();
        foreach (var pilot in pilots)
        {
            newPilots[pilot.Callsign] = pilot;
        }

        using (await _semaphore.LockAsync(cancellationToken))
        {
            _pilots = newPilots;
        }
    }

    public async Task<VatsimPilot?> Find(string callsign, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _pilots.GetValueOrDefault(callsign);
        }
    }
}
