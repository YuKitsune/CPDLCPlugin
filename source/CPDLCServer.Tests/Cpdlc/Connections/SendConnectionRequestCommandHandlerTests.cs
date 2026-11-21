using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Cpdlc.Connections;

public class SendConnectionRequestCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsConnectionRequestUplink()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var messageIdProvider = new TestMessageIdProvider();
        messageIdProvider.SetNextId(5);
        var clock = new TestClock();

        var handler = new SendConnectionRequestCommandHandler(acarsConnectionManager, messageIdProvider, clock, new TestUnitOfWork(), Logger.None);

        var command = new SendConnectionRequestCommand("UAL123", "YBBB");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var acarsConnection = (TestAcarsConnection)await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        var uplink = Assert.Single(acarsConnection.SentMessages);
        Assert.Equal("UAL123", uplink.Recipient);
        Assert.Equal("CONNECTION REQUESTED @YBBB@", uplink.Content);
        Assert.Equal(5, uplink.MessageId);
        Assert.Null(uplink.MessageReference);
        Assert.Equal(CpdlcUplinkResponseType.NoResponse, uplink.ResponseType);
        Assert.Equal(clock.UtcNow(), uplink.Sent);
    }

    [Fact]
    public async Task Handle_SendsUplinkViaConnectionForSelectedStation()
    {
        // The uplink must go through the ACARS connection of the selected ATSU, not another acarsConnection.
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var messageIdProvider = new TestMessageIdProvider();
        var clock = new TestClock();

        var handler = new SendConnectionRequestCommandHandler(acarsConnectionManager, messageIdProvider, clock, new TestUnitOfWork(), Logger.None);

        var command = new SendConnectionRequestCommand("UAL123", "YMMM");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var ymmmConnection = (TestAcarsConnection)await acarsConnectionManager.GetAcarsConnection("hoppies-ymmm", CancellationToken.None);
        var ybbbConnection = (TestAcarsConnection)await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);

        var uplink = Assert.Single(ymmmConnection.SentMessages);
        Assert.Equal("CONNECTION REQUESTED @YMMM@", uplink.Content);
        Assert.Empty(ybbbConnection.SentMessages);
    }

    [Fact]
    public async Task Handle_MatchesStationIdIgnoringCase()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var messageIdProvider = new TestMessageIdProvider();
        var clock = new TestClock();

        var handler = new SendConnectionRequestCommandHandler(acarsConnectionManager, messageIdProvider, clock, new TestUnitOfWork(), Logger.None);

        var command = new SendConnectionRequestCommand("UAL123", "ybbb");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var acarsConnection = (TestAcarsConnection)await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        Assert.Single(acarsConnection.SentMessages);
    }

    [Fact]
    public async Task Handle_ThrowsWhenNoConnectionForStation()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var messageIdProvider = new TestMessageIdProvider();
        var clock = new TestClock();

        var handler = new SendConnectionRequestCommandHandler(acarsConnectionManager, messageIdProvider, clock, new TestUnitOfWork(), Logger.None);

        var command = new SendConnectionRequestCommand("UAL123", "YPPP");

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(command, CancellationToken.None));

        var ybbbConnection = (TestAcarsConnection)await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        var ymmmConnection = (TestAcarsConnection)await acarsConnectionManager.GetAcarsConnection("hoppies-ymmm", CancellationToken.None);
        Assert.Empty(ybbbConnection.SentMessages);
        Assert.Empty(ymmmConnection.SentMessages);
    }
}
