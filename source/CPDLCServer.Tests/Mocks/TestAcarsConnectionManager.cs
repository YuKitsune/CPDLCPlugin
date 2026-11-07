using CPDLCServer.AcarsConnections;
using CPDLCServer.AcarsConnections.ConnectionSettings;

namespace CPDLCServer.Tests.Mocks;

public class TestAcarsConnectionManager : IAcarsConnectionManager
{
    private readonly Dictionary<string, IAcarsConnection> _connections = new();
    private readonly Dictionary<string, AcarsConfiguration> _configurations = new();

    public TestAcarsConnectionManager()
    {
        AddConnection("hoppies-ybbb", new TestAcarsConnection("YBBB"));
        AddConnection("hoppies-ymmm", new TestAcarsConnection("YMMM"));
        AddConfiguration(CreateConfiguration("YBBB", 10));
        AddConfiguration(CreateConfiguration("YMMM", 20));
    }

    public void AddConnection(string acarsConnectionId, IAcarsConnection connection)
    {
        _connections[acarsConnectionId] = connection;
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

    public Task<IAcarsConnection> GetAcarsConnection(string acarsConnectionId, CancellationToken cancellationToken)
    {
        if (!_connections.TryGetValue(acarsConnectionId, out var connection))
        {
            throw new ConfigurationNotFoundException(acarsConnectionId);
        }
        return Task.FromResult(connection);
    }

    public Task<IAcarsConnection> GetAcarsConnectionForStation(string stationId, CancellationToken cancellationToken)
    {
        var connection = _connections.Values.FirstOrDefault(c => c.StationId == stationId);
        if (connection is null)
        {
            throw new Exception($"No ACARS connection found for station '{stationId}'");
        }
        return Task.FromResult(connection);
    }

    public Task<AcarsConfiguration> GetConfigurationForStation(string stationId, CancellationToken cancellationToken)
    {
        if (!_configurations.TryGetValue(stationId, out var configuration))
        {
            throw new Exception($"No ACARS configuration found for station '{stationId}'");
        }
        return Task.FromResult(configuration);
    }

    public Task<string?> FindAcarsConnectionId(string stationId, CancellationToken cancellationToken)
    {
        var acarsConnectionId = _connections
            .Where(c => string.Equals(c.Value.StationId, stationId, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Key)
            .FirstOrDefault();

        return Task.FromResult(acarsConnectionId);
    }

    public bool ClientExists(string acarsConnectionId)
    {
        return _connections.ContainsKey(acarsConnectionId);
    }

    // The calls to Start and Stop, in order
    public List<string> StartStopCalls { get; } = [];
    public List<AcarsConnectionSettings> StartedConnections { get; } = [];
    public List<Guid> StoppedConnections { get; } = [];

    public string[] GetAcarsConnectionIds()
    {
        return _connections.Keys.ToArray();
    }

    public bool IsRunning(Guid settingsId)
    {
        return StartedConnections.Any(s => s.Id == settingsId);
    }

    public Task Start(AcarsConnectionSettings settings, CancellationToken cancellationToken)
    {
        StartStopCalls.Add($"Start {settings.Id}");
        StartedConnections.Add(settings);
        return Task.CompletedTask;
    }

    public Task Stop(Guid settingsId, CancellationToken cancellationToken)
    {
        StartStopCalls.Add($"Stop {settingsId}");
        StoppedConnections.Add(settingsId);
        StartedConnections.RemoveAll(s => s.Id == settingsId);
        return Task.CompletedTask;
    }
}
