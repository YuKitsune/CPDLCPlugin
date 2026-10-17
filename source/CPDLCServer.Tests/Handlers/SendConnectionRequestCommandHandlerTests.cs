using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class SendConnectionRequestCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsConnectionRequestUplink()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        messageIdProvider.SetNextId(5);
        var clock = new TestClock();

        var handler = new SendConnectionRequestCommandHandler(clientManager, messageIdProvider, clock, Logger.None);

        var command = new SendConnectionRequestCommand("UAL123", "YBBB");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var client = (TestAcarsClient)await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        var uplink = Assert.Single(client.SentMessages);
        Assert.Equal("UAL123", uplink.Recipient);
        Assert.Equal("CONNECTION REQUESTED @YBBB@", uplink.Content);
        Assert.Equal(5, uplink.MessageId);
        Assert.Null(uplink.MessageReference);
        Assert.Equal(CpdlcUplinkResponseType.NoResponse, uplink.ResponseType);
        Assert.Equal(clock.UtcNow(), uplink.Sent);
    }

    [Fact]
    public async Task Handle_SendsUplinkViaClientForSelectedStation()
    {
        // The uplink must go through the ACARS client of the selected ATSU, not another client.
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var clock = new TestClock();

        var handler = new SendConnectionRequestCommandHandler(clientManager, messageIdProvider, clock, Logger.None);

        var command = new SendConnectionRequestCommand("UAL123", "YMMM");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var ymmmClient = (TestAcarsClient)await clientManager.GetAcarsClient("hoppies-ymmm", CancellationToken.None);
        var ybbbClient = (TestAcarsClient)await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);

        var uplink = Assert.Single(ymmmClient.SentMessages);
        Assert.Equal("CONNECTION REQUESTED @YMMM@", uplink.Content);
        Assert.Empty(ybbbClient.SentMessages);
    }

    [Fact]
    public async Task Handle_MatchesStationIdIgnoringCase()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var clock = new TestClock();

        var handler = new SendConnectionRequestCommandHandler(clientManager, messageIdProvider, clock, Logger.None);

        var command = new SendConnectionRequestCommand("UAL123", "ybbb");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var client = (TestAcarsClient)await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        Assert.Single(client.SentMessages);
    }

    [Fact]
    public async Task Handle_ThrowsWhenNoClientForStation()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var clock = new TestClock();

        var handler = new SendConnectionRequestCommandHandler(clientManager, messageIdProvider, clock, Logger.None);

        var command = new SendConnectionRequestCommand("UAL123", "YPPP");

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(command, CancellationToken.None));

        var ybbbClient = (TestAcarsClient)await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        var ymmmClient = (TestAcarsClient)await clientManager.GetAcarsClient("hoppies-ymmm", CancellationToken.None);
        Assert.Empty(ybbbClient.SentMessages);
        Assert.Empty(ymmmClient.SentMessages);
    }
}
