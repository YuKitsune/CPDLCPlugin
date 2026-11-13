using CPDLCServer.Settings;

namespace CPDLCServer.Tests.Mocks;

public class TestServerSettingsProvider(ServerSettings settings) : IServerSettingsProvider
{
    ServerSettings _settings = settings;

    // The settings are given in the constructor, so there are always settings
    public Task SeedDefaults(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

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
