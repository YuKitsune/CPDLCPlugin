using CPDLCServer.Settings;

namespace CPDLCServer.Tests.Mocks;

public class TestServerSettingsProvider(ServerSettings settings) : IServerSettingsProvider
{
    ServerSettings _settings = settings;

    public Task<ServerSettings> Load(CancellationToken cancellationToken)
    {
        return Task.FromResult(_settings);
    }

    public Task Save(ServerSettings settings, CancellationToken cancellationToken)
    {
        _settings = settings;
        return Task.CompletedTask;
    }
}
