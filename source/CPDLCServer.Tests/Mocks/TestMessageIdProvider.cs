using CPDLCServer.Cpdlc.Uplinks;

namespace CPDLCServer.Tests.Mocks;

public class TestMessageIdProvider : IMessageIdProvider
{
    private int _nextId = 1;

    public Task<int> GetNextMessageId(string stationId, string callsign, CancellationToken cancellationToken)
    {
        return Task.FromResult(_nextId++);
    }

    public void SetNextId(int id)
    {
        _nextId = id;
    }
}