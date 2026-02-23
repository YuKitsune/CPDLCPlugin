using CPDLCServer.Extensions;

namespace CPDLCServer.Persistence;

public class InMemoryAcarsConnectedCallsignsRepository : IAcarsConnectedCallsignsRepository
{
    readonly HashSet<string> _callsigns = new(StringComparer.OrdinalIgnoreCase);
    readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<string[]> All(CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _callsigns.ToArray();
        }
    }

    public async Task<bool> Update(string[] callsigns, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            var newCallsignsSet = new HashSet<string>(callsigns, StringComparer.OrdinalIgnoreCase);
            var changed = !_callsigns.SetEquals(newCallsignsSet);

            if (!changed)
                return changed;

            _callsigns.Clear();
            foreach (var callsign in callsigns)
            {
                _callsigns.Add(callsign);
            }

            return changed;
        }
    }
}
