using CPDLCServer.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Settings;

public interface IServerSettingsProvider
{
    /// <summary>
    /// Adds the default settings when the database has no settings, for example at the first start
    /// </summary>
    Task SeedDefaults(CancellationToken cancellationToken);
    Task<ServerSettings> Load(CancellationToken cancellationToken);

    // Does not change the banner
    Task Save(ServerSettings settings, CancellationToken cancellationToken);

    // Does not load the image
    Task<BrandingInfo> LoadBranding(CancellationToken cancellationToken);

    Task<(byte[] Image, string ContentType)?> LoadBanner(CancellationToken cancellationToken);
}

public class ServerSettingsProvider(CpdlcDbContext dbContext) : IServerSettingsProvider
{
    public async Task SeedDefaults(CancellationToken cancellationToken)
    {
        if (await dbContext.ServerSettings.AnyAsync(cancellationToken))
            return;

        dbContext.ServerSettings.Add(new ServerSettings());
    }

    public async Task<ServerSettings> Load(CancellationToken cancellationToken)
    {
        await dbContext.ServerSettings.LoadAsync(cancellationToken);
        return dbContext.ServerSettings.Local.Single();
    }

    public async Task Save(ServerSettings settings, CancellationToken cancellationToken)
    {
        var current = await Load(cancellationToken);

        // The settings form does not have the banner, so keep the current banner
        var changes = settings with
        {
            BannerImage = current.BannerImage,
            BannerContentType = current.BannerContentType,
            BannerVersion = current.BannerVersion
        };

        dbContext.Entry(current).CurrentValues.SetValues(changes);
    }

    public async Task<BrandingInfo> LoadBranding(CancellationToken cancellationToken)
    {
        return await dbContext.ServerSettings
            .AsNoTracking()
            .Select(s => new BrandingInfo(s.ServerName, s.Motd, s.BannerImage != null, s.BannerVersion))
            .SingleAsync(cancellationToken);
    }

    public async Task<(byte[] Image, string ContentType)?> LoadBanner(CancellationToken cancellationToken)
    {
        var banner = await dbContext.ServerSettings
            .AsNoTracking()
            .Select(s => new { s.BannerImage, s.BannerContentType })
            .SingleAsync(cancellationToken);

        if (banner.BannerImage is null || banner.BannerContentType is null)
            return null;

        return (banner.BannerImage, banner.BannerContentType);
    }
}
