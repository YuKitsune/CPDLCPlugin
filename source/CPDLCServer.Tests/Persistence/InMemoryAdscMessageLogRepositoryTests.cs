using CPDLCServer.Model;
using CPDLCServer.Persistence;

namespace CPDLCServer.Tests.Persistence;

public class InMemoryAdscMessageLogRepositoryTests
{
    static readonly DateTimeOffset Start = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task All_ReturnsNewestEntriesFirst()
    {
        // Arrange
        var repository = new InMemoryAdscMessageLogRepository();
        var first = CreateEntry(0);
        var second = CreateEntry(1);

        // Act
        await repository.Add(first, CancellationToken.None);
        await repository.Add(second, CancellationToken.None);

        // Assert
        Assert.Equal([second, first], await repository.All(CancellationToken.None));
    }

    [Fact]
    public async Task Add_WhenFull_RemovesOldestEntry()
    {
        // Arrange
        var repository = new InMemoryAdscMessageLogRepository();
        for (var i = 0; i < InMemoryAdscMessageLogRepository.Capacity; i++)
            await repository.Add(CreateEntry(i), CancellationToken.None);

        var newest = CreateEntry(InMemoryAdscMessageLogRepository.Capacity);

        // Act
        await repository.Add(newest, CancellationToken.None);

        // Assert
        var entries = await repository.All(CancellationToken.None);
        Assert.Equal(InMemoryAdscMessageLogRepository.Capacity, entries.Length);
        Assert.Equal(newest, entries.First());
        Assert.Equal(CreateEntry(1), entries.Last());
    }

    static AdscMessageLogEntry CreateEntry(int index)
    {
        return new AdscMessageLogEntry(
            Start.AddMinutes(index),
            "Hoppies/YBBB",
            "YBBB",
            "UAL123",
            AdscMessageDirection.Uplink,
            $"REQUEST PERIODIC {index}");
    }
}
