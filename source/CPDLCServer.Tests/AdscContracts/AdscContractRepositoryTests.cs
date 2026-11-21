using CPDLCServer.AdscContracts;
using CPDLCServer.AdscContracts.Reports;
using CPDLCServer.Tests.Mocks;

namespace CPDLCServer.Tests.AdscContracts;

public class AdscContractRepositoryTests
{
    static readonly DateTimeOffset Start = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Find_ReturnsSavedContract()
    {
        // Arrange
        using var database = new TestDatabase();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(15), Start.AddMinutes(-10));
        contract.RequestDefaultContract(TimeSpan.FromMinutes(10));
        contract.ReportReceived(new AdscReport(Start.AddMinutes(-5), -33.9461, 151.1772, 37000, AdscReportSource.VATSIM));
        contract.RequestSelectedContract(TimeSpan.FromMinutes(2));
        contract.RequestDemandReport();
        contract.LogActivity(Start);

        await using (var dbContext = database.CreateDbContext())
        {
            await new AdscContractRepository(dbContext).Add(new("UAL123", "YBBB"), contract, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Act
        await using var readDbContext = database.CreateDbContext();
        var saved = await new AdscContractRepository(readDbContext).Find(new("UAL123", "YBBB"), CancellationToken.None);

        // Assert
        Assert.NotNull(saved);
        Assert.Equal(TimeSpan.FromMinutes(10), saved.DefaultReportingInterval);
        Assert.Equal(TimeSpan.FromMinutes(2), saved.ReportingInterval);
        Assert.Equal(AdscContractState.Accepted, saved.DefaultContractState);
        Assert.Equal(TimeSpan.FromMinutes(2), saved.SelectedReportingInterval);
        Assert.Equal(AdscContractState.Sent, saved.SelectedContractState);
        Assert.Equal(AdscContractState.Sent, saved.DemandContractState);
        Assert.Equal(Start.AddMinutes(-10), saved.Created);
        Assert.Equal(Start, saved.LastActivity);
        Assert.Equal(Start.AddMinutes(-5), saved.LastReportReceived);
        Assert.Equal(AdscReportSource.VATSIM, saved.ReportingSource);
    }

    [Fact]
    public async Task Remove_DeletesContract()
    {
        // Arrange
        using var database = new TestDatabase();
        await using (var dbContext = database.CreateDbContext())
        {
            await new AdscContractRepository(dbContext).Add(new("UAL123", "YBBB"), new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10), DateTimeOffset.MinValue), CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Act
        bool removed;
        await using (var dbContext = database.CreateDbContext())
        {
            removed = await new AdscContractRepository(dbContext).Remove(new("UAL123", "YBBB"), CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        Assert.True(removed);
        await using var readDbContext = database.CreateDbContext();
        Assert.Empty(await new AdscContractRepository(readDbContext).All(CancellationToken.None));
    }
}
