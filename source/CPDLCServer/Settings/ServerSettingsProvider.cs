using CPDLCServer.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Settings;

public interface IServerSettingsProvider
{
    Task<ServerSettings> Load(CancellationToken cancellationToken);
    Task Save(ServerSettings settings, CancellationToken cancellationToken);
}

// Reads and changes the settings with the database context of the current lifetime scope.
// The changes are saved when the unit of work of the scope is committed.
public class ServerSettingsProvider(CpdlcDbContext dbContext) : IServerSettingsProvider
{
    // Adds the default settings when the database has no settings, for example at the first start
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
        dbContext.Entry(current).CurrentValues.SetValues(settings);
    }
}
