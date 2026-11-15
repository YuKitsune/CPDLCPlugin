using CPDLCServer.Cpdlc.Uplinks;

namespace CPDLCServer.Tests.Mocks;

public class TestMessageIdProvider : IMessageIdProvider
{
    private int _nextId = 1;

    public List<(string AcarsConnectionId, string Callsign)> Removed { get; } = [];

    public Task<int> GetNextMessageId(string stationId, string callsign, CancellationToken cancellationToken)
    {
        return Task.FromResult(_nextId++);
    }

    public Task Remove(string acarsConnectionId, string callsign, CancellationToken cancellationToken)
    {
        Removed.Add((acarsConnectionId, callsign));
        return Task.CompletedTask;
    }

    public void SetNextId(int id)
    {
        _nextId = id;
    }
}
