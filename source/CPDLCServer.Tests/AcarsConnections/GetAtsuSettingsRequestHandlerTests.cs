using CPDLCServer.AcarsConnections;
using CPDLCServer.Tests.Mocks;

namespace CPDLCServer.Tests.AcarsConnections;

public class GetAtsuSettingsRequestHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSettingsForStation()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        acarsConnectionManager.AddConfiguration(TestAcarsConnectionManager.CreateConfiguration("NZZO", 27));
        var handler = new GetAtsuSettingsRequestHandler(acarsConnectionManager);

        // Act
        var result = await handler.Handle(new GetAtsuSettingsRequest("NZZO"), CancellationToken.None);

        // Assert
        Assert.Equal("NZZO", result.StationId);
        Assert.Equal(TimeSpan.FromMinutes(27), result.DefaultAdscReportingInterval);
    }

    [Fact]
    public async Task Handle_WhenStationNotConfigured_Throws()
    {
        // Arrange
        var handler = new GetAtsuSettingsRequestHandler(new TestAcarsConnectionManager());

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(new GetAtsuSettingsRequest("KZAK"), CancellationToken.None));
    }
}
