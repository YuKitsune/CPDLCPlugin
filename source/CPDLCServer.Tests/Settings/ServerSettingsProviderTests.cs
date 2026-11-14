using CPDLCServer.Settings;
using CPDLCServer.Tests.Mocks;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Tests.Settings;

public class ServerSettingsProviderTests
{
    [Fact]
    public async Task SeedDefaults_WhenNoSettings_AddsDefaults()
    {
        // Arrange
        using var database = new TestDatabase();

        // Act
        await Seed(database);

        // Assert
        await using var dbContext = database.CreateDbContext();
        var settings = await new ServerSettingsProvider(dbContext).Load(CancellationToken.None);
        Assert.Equal(new ServerSettings(), settings);
    }

    [Fact]
    public async Task SeedDefaults_WhenSettingsExist_DoesNotChangeThem()
    {
        // Arrange
        using var database = new TestDatabase();
        await Seed(database);
        await SaveAndCommit(database, new ServerSettings { AdscReportRetentionHours = 48 });

        // Act
        await Seed(database);

        // Assert
        await using var dbContext = database.CreateDbContext();
        Assert.Equal(1, await dbContext.ServerSettings.CountAsync());
        var settings = await new ServerSettingsProvider(dbContext).Load(CancellationToken.None);
        Assert.Equal(48, settings.AdscReportRetentionHours);
    }

    [Fact]
    public async Task Save_ThenCommit_ThenLoad_ReturnsSavedSettings()
    {
        // Arrange
        using var database = new TestDatabase();
        await Seed(database);
        var settings = new ServerSettings
        {
            ConnectivityCheckIntervalMinutes = 5,
            AircraftConnectionTimeoutMinutes = 20,
            VatsimStatusEnabled = true,
            VatsimStatusUrl = new Uri("https://example.com/vatsim-data.json")
        };

        // Act
        await SaveAndCommit(database, settings);

        // Assert
        await using var dbContext = database.CreateDbContext();
        Assert.Equal(settings, await new ServerSettingsProvider(dbContext).Load(CancellationToken.None));
    }

    [Fact]
    public async Task Save_WithoutCommit_DoesNotChangeTheDatabase()
    {
        // Arrange
        using var database = new TestDatabase();
        await Seed(database);

        // Act
        await using (var dbContext = database.CreateDbContext())
        {
            await new ServerSettingsProvider(dbContext).Save(
                new ServerSettings { AdscReportRetentionHours = 48 },
                CancellationToken.None);
        }

        // Assert
        await using var otherDbContext = database.CreateDbContext();
        var settings = await new ServerSettingsProvider(otherDbContext).Load(CancellationToken.None);
        Assert.Equal(new ServerSettings().AdscReportRetentionHours, settings.AdscReportRetentionHours);
    }

    [Fact]
    public async Task Load_AfterSaveInTheSameScope_ReturnsTheUnsavedSettings()
    {
        // Arrange
        using var database = new TestDatabase();
        await Seed(database);
        await using var dbContext = database.CreateDbContext();
        var provider = new ServerSettingsProvider(dbContext);

        // Act
        await provider.Save(new ServerSettings { AdscReportRetentionHours = 48 }, CancellationToken.None);

        // Assert
        Assert.Equal(48, (await provider.Load(CancellationToken.None)).AdscReportRetentionHours);
    }

    static async Task Seed(TestDatabase database)
    {
        await using var dbContext = database.CreateDbContext();
        await new ServerSettingsProvider(dbContext).SeedDefaults(CancellationToken.None);
        await dbContext.SaveChangesAsync();
    }

    static async Task SaveAndCommit(TestDatabase database, ServerSettings settings)
    {
        await using var dbContext = database.CreateDbContext();
        await new ServerSettingsProvider(dbContext).Save(settings, CancellationToken.None);
        await dbContext.SaveChangesAsync();
    }
}
