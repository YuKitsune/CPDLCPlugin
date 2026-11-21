using System.Security.Cryptography;
using CPDLCServer.AcarsConnections.ConnectionSettings;
using CPDLCServer.Security;
using CPDLCServer.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

namespace CPDLCServer.Tests.AcarsConnections.ConnectionSettings;

public sealed class AcarsConnectionCommandHandlerTests : IDisposable
{
    readonly MasterKeyEncryption _masterKeyEncryption = new(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    readonly TestDatabase _database = new();
    readonly TestAcarsConnectionManager _acarsConnectionManager = new();

    [Fact]
    public async Task Create_SavesSettingsWithEncryptedCode_AndStartsConnection()
    {
        // Arrange
        var connection = new AcarsConnectionSettings { StationIdentifier = "ybbb" };

        // Act
        Guid id;
        await using (var dbContext = _database.CreateDbContext())
        {
            var handler = new CreateAcarsConnectionCommandHandler(dbContext, _masterKeyEncryption, _acarsConnectionManager, new TestUnitOfWork(), Logger.None);
            id = await handler.Handle(new CreateAcarsConnectionCommand(connection, "LOGON123"), CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        var saved = Assert.Single(await AllSettings());
        Assert.Equal(id, saved.Id);
        Assert.Equal("YBBB", saved.StationIdentifier);
        Assert.NotEqual("LOGON123", saved.EncryptedAuthenticationCode);
        Assert.Equal("LOGON123", _masterKeyEncryption.Decrypt(saved.EncryptedAuthenticationCode));
        Assert.Equal([id], _acarsConnectionManager.StartedConnections.Select(s => s.Id));
    }

    [Fact]
    public async Task Create_WhenDisabled_DoesNotStartConnection()
    {
        // Arrange
        var connection = new AcarsConnectionSettings { StationIdentifier = "YBBB", Enabled = false };

        // Act
        await using (var dbContext = _database.CreateDbContext())
        {
            var handler = new CreateAcarsConnectionCommandHandler(dbContext, _masterKeyEncryption, _acarsConnectionManager, new TestUnitOfWork(), Logger.None);
            await handler.Handle(new CreateAcarsConnectionCommand(connection, "LOGON123"), CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        Assert.Single(await AllSettings());
        Assert.Empty(_acarsConnectionManager.StartedConnections);
    }

    [Fact]
    public async Task Create_WithoutAuthenticationCode_Throws()
    {
        // Arrange
        await using var dbContext = _database.CreateDbContext();
        var handler = new CreateAcarsConnectionCommandHandler(dbContext, _masterKeyEncryption, _acarsConnectionManager, new TestUnitOfWork(), Logger.None);
        var connection = new AcarsConnectionSettings { StationIdentifier = "YBBB" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new CreateAcarsConnectionCommand(connection, null), CancellationToken.None));
        Assert.Empty(dbContext.AcarsConnections.Local);
    }

    [Fact]
    public async Task Update_StopsThenStartsConnectionWithNewSettings()
    {
        // Arrange
        var id = await AddSettings(enabled: true);
        var changes = new AcarsConnectionSettings { StationIdentifier = "YBBB", DefaultAdscReportingInterval = 5, Notes = "Brisbane FIR" };

        // Act
        await Update(id, changes, null);

        // Assert
        Assert.Equal([$"Stop {id}", $"Start {id}"], _acarsConnectionManager.StartStopCalls);
        var saved = Assert.Single(await AllSettings());
        Assert.Equal(5, saved.DefaultAdscReportingInterval);
        Assert.Equal("Brisbane FIR", saved.Notes);
    }

    [Fact]
    public async Task Update_WhenDisabled_StopsConnection()
    {
        // Arrange
        var id = await AddSettings(enabled: true);

        // Act
        await Update(id, new AcarsConnectionSettings { StationIdentifier = "YBBB", Enabled = false }, null);

        // Assert
        Assert.Equal([$"Stop {id}"], _acarsConnectionManager.StartStopCalls);
        Assert.False(Assert.Single(await AllSettings()).Enabled);
    }

    [Fact]
    public async Task Update_WithoutAuthenticationCode_KeepsCurrentCode()
    {
        // Arrange
        var id = await AddSettings(enabled: true);

        // Act
        await Update(id, new AcarsConnectionSettings { StationIdentifier = "YBBB" }, "");

        // Assert
        var saved = Assert.Single(await AllSettings());
        Assert.Equal("LOGON123", _masterKeyEncryption.Decrypt(saved.EncryptedAuthenticationCode));
    }

    [Fact]
    public async Task Update_WithAuthenticationCode_ReplacesCode()
    {
        // Arrange
        var id = await AddSettings(enabled: true);

        // Act
        await Update(id, new AcarsConnectionSettings { StationIdentifier = "YBBB" }, "NEWCODE");

        // Assert
        var saved = Assert.Single(await AllSettings());
        Assert.Equal("NEWCODE", _masterKeyEncryption.Decrypt(saved.EncryptedAuthenticationCode));
    }

    [Fact]
    public async Task Delete_RemovesSettingsAndStopsConnection()
    {
        // Arrange
        var id = await AddSettings(enabled: true);

        // Act
        await using (var dbContext = _database.CreateDbContext())
        {
            var handler = new DeleteAcarsConnectionCommandHandler(dbContext, _acarsConnectionManager, new TestUnitOfWork(), Logger.None);
            await handler.Handle(new DeleteAcarsConnectionCommand(id), CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        Assert.Empty(await AllSettings());
        Assert.Equal([id], _acarsConnectionManager.StoppedConnections);
    }

    [Fact]
    public async Task Delete_WhenNotFound_DoesNothing()
    {
        // Arrange
        await using var dbContext = _database.CreateDbContext();
        var handler = new DeleteAcarsConnectionCommandHandler(dbContext, _acarsConnectionManager, new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new DeleteAcarsConnectionCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.Empty(_acarsConnectionManager.StoppedConnections);
    }

    async Task Update(Guid id, AcarsConnectionSettings changes, string? authenticationCode)
    {
        await using var dbContext = _database.CreateDbContext();
        var handler = new UpdateAcarsConnectionCommandHandler(dbContext, _masterKeyEncryption, _acarsConnectionManager, new TestUnitOfWork(), Logger.None);
        await handler.Handle(new UpdateAcarsConnectionCommand(id, changes, authenticationCode), CancellationToken.None);
        await dbContext.SaveChangesAsync();
    }

    async Task<Guid> AddSettings(bool enabled)
    {
        var settings = new AcarsConnectionSettings
        {
            Enabled = enabled,
            StationIdentifier = "YBBB",
            EncryptedAuthenticationCode = _masterKeyEncryption.Encrypt("LOGON123")
        };

        await using var dbContext = _database.CreateDbContext();
        dbContext.AcarsConnections.Add(settings);
        await dbContext.SaveChangesAsync();
        return settings.Id;
    }

    async Task<AcarsConnectionSettings[]> AllSettings()
    {
        await using var dbContext = _database.CreateDbContext();
        return await dbContext.AcarsConnections.ToArrayAsync();
    }

    public void Dispose()
    {
        _database.Dispose();
    }
}
