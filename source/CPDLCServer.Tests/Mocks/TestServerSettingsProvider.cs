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
        _settings = settings with
        {
            BannerImage = _settings.BannerImage,
            BannerContentType = _settings.BannerContentType,
            BannerVersion = _settings.BannerVersion
        };
        return Task.CompletedTask;
    }

    public Task<BrandingInfo> LoadBranding(CancellationToken cancellationToken)
    {
        return Task.FromResult(new BrandingInfo(_settings.ServerName, _settings.Motd, _settings.BannerImage is not null, _settings.BannerVersion));
    }

    public Task<(byte[] Image, string ContentType)?> LoadBanner(CancellationToken cancellationToken)
    {
        (byte[] Image, string ContentType)? banner = _settings is { BannerImage: not null, BannerContentType: not null }
            ? (_settings.BannerImage, _settings.BannerContentType)
            : null;
        return Task.FromResult(banner);
    }
}
