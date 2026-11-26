using CPDLCServer.AcarsConnections;
using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Cpdlc.Connections;

public class ControlMessageReceivedNotificationHandlerTests
{
    [Fact]
    public async Task Handle_LogonRequest_DispatchesLogonCommand()
    {
        // Arrange
        var clock = new TestClock();
        var mediator = Substitute.For<IMediator>();
        var handler = new ControlMessageReceivedNotificationHandler(mediator, Logger.None);

        var notification = new ControlMessageReceivedNotification(
            "hoppies-ybbb",
            new ReceivedLogonRequest("QFA1", clock.UtcNow()));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            new LogonCommand("QFA1", "hoppies-ybbb"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Logoff_TerminatesConnection()
    {
        // Arrange
        var clock = new TestClock();
        var mediator = Substitute.For<IMediator>();
        var handler = new ControlMessageReceivedNotificationHandler(mediator, Logger.None);

        var notification = new ControlMessageReceivedNotification(
            "hoppies-ybbb",
            new ReceivedLogoff("QFA1", clock.UtcNow()));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            new TerminateConnectionRequest("QFA1", "hoppies-ybbb"),
            Arg.Any<CancellationToken>());
    }
}
