using CPDLCServer.AcarsConnections.Stations;
using CPDLCServer.AdscContracts.Reports;
using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Logging;
using CPDLCServer.Settings;
using CPDLCServer.VatsimStatus;

namespace CPDLCServer.Tests.Logging;

public class LogAreaTests
{
    [Theory]
    [InlineData(typeof(LogonCommand), LogArea.Cpdlc)]
    [InlineData(typeof(ExpireAdscReportsRequest), LogArea.Adsc)]
    [InlineData(typeof(ProduceVatsimAdscReportsRequest), LogArea.Adsc)]
    [InlineData(typeof(RefreshAcarsCallsignsRequest), LogArea.Acars)]
    [InlineData(typeof(SaveServerSettingsCommand), LogArea.System)]
    public void ForType_UsesTheNamespaceOfTheType(Type type, string expectedArea)
    {
        // Act
        var area = LogArea.ForType(type);

        // Assert
        Assert.Equal(expectedArea, area);
    }
}
