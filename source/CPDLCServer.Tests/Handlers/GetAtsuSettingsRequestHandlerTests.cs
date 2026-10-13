using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Tests.Mocks;

namespace CPDLCServer.Tests.Handlers;

public class GetAtsuSettingsRequestHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSettingsForStation()
    {
        // Arrange
        var clientManager = new TestClientManager();
        clientManager.AddConfiguration(TestClientManager.CreateConfiguration("NZZO", 27));
        var handler = new GetAtsuSettingsRequestHandler(clientManager);

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
        var handler = new GetAtsuSettingsRequestHandler(new TestClientManager());

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(new GetAtsuSettingsRequest("KZAK"), CancellationToken.None));
    }
}
