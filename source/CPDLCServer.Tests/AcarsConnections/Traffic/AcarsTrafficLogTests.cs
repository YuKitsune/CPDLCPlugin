using CPDLCServer.AcarsConnections.Traffic;

namespace CPDLCServer.Tests.AcarsConnections.Traffic;

public class AcarsTrafficLogTests
{
    [Fact]
    public void Get_ReturnsTheNewestEntriesFirst()
    {
        // Arrange
        var trafficLog = new AcarsTrafficLog();

        // Act
        trafficLog.Add("Hoppies/YBBB", CreateEntry("first"));
        trafficLog.Add("Hoppies/YBBB", CreateEntry("second"));

        // Assert
        Assert.Equal(["second", "first"], trafficLog.Get("Hoppies/YBBB").Select(e => e.Response));
    }

    [Fact]
    public void Add_KeepsOnlyTheMostRecentEntriesOfEachConnection()
    {
        // Arrange
        var trafficLog = new AcarsTrafficLog(capacity: 2);

        // Act
        trafficLog.Add("Hoppies/YBBB", CreateEntry("first"));
        trafficLog.Add("Hoppies/YBBB", CreateEntry("second"));
        trafficLog.Add("Hoppies/YBBB", CreateEntry("third"));
        trafficLog.Add("Hoppies/YMMM", CreateEntry("other"));

        // Assert
        Assert.Equal(["third", "second"], trafficLog.Get("Hoppies/YBBB").Select(e => e.Response));
        Assert.Equal(["other"], trafficLog.Get("Hoppies/YMMM").Select(e => e.Response));
    }

    [Fact]
    public void Get_WhenNoTraffic_ReturnsNoEntries()
    {
        // Arrange
        var trafficLog = new AcarsTrafficLog();

        // Act & Assert
        Assert.Empty(trafficLog.Get("Hoppies/YBBB"));
    }

    [Fact]
    public void FormatRequest_ReplacesTheAuthenticationCode()
    {
        // Arrange
        var fields = new Dictionary<string, string>
        {
            ["logon"] = "SECRET123",
            ["from"] = "YBBB",
            ["type"] = "poll"
        };

        // Act
        var request = AcarsTrafficLog.FormatRequest(fields);

        // Assert
        Assert.DoesNotContain("SECRET123", request);
        Assert.Equal($"logon={AcarsTrafficLog.RedactedValue}\nfrom=YBBB\ntype=poll", request);
    }

    [Theory]
    [InlineData("poll", "ok", null, true)]
    [InlineData("poll", "ok {UAL123 cpdlc {/data2/1//Y/REQUEST DESCENT}}", null, false)]
    [InlineData("poll", null, "Timeout", false)]
    [InlineData("cpdlc", "ok", null, false)]
    public void IsEmptyPoll_IsTrueOnlyForAPollWithNoMessages(string type, string? response, string? error, bool expected)
    {
        // Arrange
        var entry = new AcarsTrafficEntry(DateTimeOffset.UtcNow, type, "type=poll", 200, response, TimeSpan.Zero, error);

        // Act & Assert
        Assert.Equal(expected, entry.IsEmptyPoll);
    }

    static AcarsTrafficEntry CreateEntry(string response)
    {
        return new AcarsTrafficEntry(DateTimeOffset.UtcNow, "poll", "type=poll", 200, response, TimeSpan.Zero, null);
    }
}
