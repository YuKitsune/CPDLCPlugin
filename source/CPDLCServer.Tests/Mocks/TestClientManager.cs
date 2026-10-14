using CPDLCServer.Clients;
using CPDLCServer.Exceptions;

namespace CPDLCServer.Tests.Mocks;

public class TestClientManager : IClientManager
{
    private readonly Dictionary<string, IAcarsClient> _clients = new();
    private readonly Dictionary<string, AcarsConfiguration> _configurations = new();

    public TestClientManager()
    {
        AddClient("hoppies-ybbb", new TestAcarsClient("YBBB"));
        AddClient("hoppies-ymmm", new TestAcarsClient("YMMM"));
        AddConfiguration(CreateConfiguration("YBBB", 10));
        AddConfiguration(CreateConfiguration("YMMM", 20));
    }

    public void AddClient(string acarsClientId, IAcarsClient client)
    {
        _clients[acarsClientId] = client;
    }

    public void AddConfiguration(HoppiesConfiguration configuration)
    {
        _configurations[configuration.StationIdentifier] = configuration;
    }

    public static HoppiesConfiguration CreateConfiguration(
        string stationId,
        int defaultAdscReportingInterval,
        int selectedAdscContractDuration = 60)
    {
        return new HoppiesConfiguration
        {
            Url = new Uri("http://test.hoppie.nl/acars"),
            AuthenticationCode = "TEST123",
            FlightSimulationNetwork = "VATSIM",
            StationIdentifier = stationId,
            DefaultAdscReportingInterval = defaultAdscReportingInterval,
            SelectedAdscContractDuration = selectedAdscContractDuration
        };
    }

    public Task<IAcarsClient> GetAcarsClient(string acarsClientId, CancellationToken cancellationToken)
    {
        if (!_clients.TryGetValue(acarsClientId, out var client))
        {
            throw new ConfigurationNotFoundException(acarsClientId);
        }
        return Task.FromResult(client);
    }

    public Task<IAcarsClient> GetAcarsClientForStation(string stationId, CancellationToken cancellationToken)
    {
        var client = _clients.Values.FirstOrDefault(c => c.StationId == stationId);
        if (client is null)
        {
            throw new Exception($"No ACARS client found for station '{stationId}'");
        }
        return Task.FromResult(client);
    }

    public Task<AcarsConfiguration> GetConfigurationForStation(string stationId, CancellationToken cancellationToken)
    {
        if (!_configurations.TryGetValue(stationId, out var configuration))
        {
            throw new Exception($"No ACARS configuration found for station '{stationId}'");
        }
        return Task.FromResult(configuration);
    }

    public bool ClientExists(string acarsClientId)
    {
        return _clients.ContainsKey(acarsClientId);
    }
}
