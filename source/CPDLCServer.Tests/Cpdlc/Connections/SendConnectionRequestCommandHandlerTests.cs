using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Cpdlc.Connections;

public class SendConnectionRequestCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsConnectionRequest()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var messageIdProvider = new TestMessageIdProvider();
        messageIdProvider.SetNextId(5);

        var handler = new SendConnectionRequestCommandHandler(acarsConnectionManager, messageIdProvider, new TestUnitOfWork(), Logger.None);

        var command = new SendConnectionRequestCommand("UAL123", "YBBB");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var acarsConnection = (TestAcarsConnection)await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        var connectionRequest = Assert.Single(acarsConnection.SentConnectionRequests);
        Assert.Equal("UAL123", connectionRequest.Callsign);
        Assert.Equal(5, connectionRequest.MessageId);
        Assert.Empty(acarsConnection.SentMessages);
    }

    [Fact]
    public async Task Handle_SendsUplinkViaConnectionForSelectedStation()
    {
        // The uplink must go through the ACARS connection of the selected ATSU, not another acarsConnection.
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var messageIdProvider = new TestMessageIdProvider();

        var handler = new SendConnectionRequestCommandHandler(acarsConnectionManager, messageIdProvider, new TestUnitOfWork(), Logger.None);

        var command = new SendConnectionRequestCommand("UAL123", "YMMM");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var ymmmConnection = (TestAcarsConnection)await acarsConnectionManager.GetAcarsConnection("hoppies-ymmm", CancellationToken.None);
        var ybbbConnection = (TestAcarsConnection)await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);

        Assert.Single(ymmmConnection.SentConnectionRequests);
        Assert.Empty(ybbbConnection.SentConnectionRequests);
    }

    [Fact]
    public async Task Handle_MatchesStationIdIgnoringCase()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var messageIdProvider = new TestMessageIdProvider();

        var handler = new SendConnectionRequestCommandHandler(acarsConnectionManager, messageIdProvider, new TestUnitOfWork(), Logger.None);

        var command = new SendConnectionRequestCommand("UAL123", "ybbb");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var acarsConnection = (TestAcarsConnection)await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        Assert.Single(acarsConnection.SentConnectionRequests);
    }

    [Fact]
    public async Task Handle_ThrowsWhenNoConnectionForStation()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var messageIdProvider = new TestMessageIdProvider();

        var handler = new SendConnectionRequestCommandHandler(acarsConnectionManager, messageIdProvider, new TestUnitOfWork(), Logger.None);

        var command = new SendConnectionRequestCommand("UAL123", "YPPP");

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(command, CancellationToken.None));

        var ybbbConnection = (TestAcarsConnection)await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        var ymmmConnection = (TestAcarsConnection)await acarsConnectionManager.GetAcarsConnection("hoppies-ymmm", CancellationToken.None);
        Assert.Empty(ybbbConnection.SentConnectionRequests);
        Assert.Empty(ymmmConnection.SentConnectionRequests);
    }
}
