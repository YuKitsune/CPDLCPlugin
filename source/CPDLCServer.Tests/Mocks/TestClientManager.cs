using CPDLCServer.Clients;
using CPDLCServer.Exceptions;

namespace CPDLCServer.Tests.Mocks;

public class TestClientManager : IClientManager
{
    private readonly Dictionary<string, IAcarsClient> _clients = new();

    public TestClientManager()
    {
        AddClient("hoppies-ybbb", new TestAcarsClient("YBBB"));
        AddClient("hoppies-ymmm", new TestAcarsClient("YMMM"));
    }

    public void AddClient(string acarsClientId, IAcarsClient client)
    {
        _clients[acarsClientId] = client;
    }

    public Task<IAcarsClient> GetAcarsClient(string acarsClientId, CancellationToken cancellationToken)
    {
        if (!_clients.TryGetValue(acarsClientId, out var client))
        {
            throw new ConfigurationNotFoundException(acarsClientId);
        }
        return Task.FromResult(client);
    }

    public bool ClientExists(string acarsClientId)
    {
        return _clients.ContainsKey(acarsClientId);
    }
}
