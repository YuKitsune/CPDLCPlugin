using CPDLCServer.Handlers;
using CPDLCServer.Hubs;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class AircraftDisconnectedNotificationHandlerTests
{
    [Fact]
    public async Task Handle_NotifiesAllConnectedControllers()
    {
        // Arrange
        var controllerManager = new TestControllerRepository();
        var controller1 = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-1",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        var controller2 = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-2",
            "YBBB",
            "BN-OCN_CTR",
            "7654321");
        await controllerManager.Add(controller1, CancellationToken.None);
        await controllerManager.Add(controller2, CancellationToken.None);

        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftDisconnectedNotificationHandler(
            controllerManager,
            hubContext,
            Logger.None);

        var notification = new AircraftDisconnected("hoppies-ybbb", "YBBB", "UAL123");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - all controllers should be notified
        hubContext.Clients.Received(1).Clients(
            Arg.Is<IReadOnlyList<string>>(ids =>
                ids.Count == 2 &&
                ids.Contains("ConnectionId-1") &&
                ids.Contains("ConnectionId-2")));

        await clientProxy.Received(1).SendCoreAsync(
            "AircraftConnectionRemoved",
            Arg.Is<object[]>(args => args.Length == 2 && args[0].ToString() == "UAL123" && args[1].ToString() == "YBBB"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotNotifyWhenNoControllersConnected()
    {
        // Arrange
        var controllerManager = new TestControllerRepository();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftDisconnectedNotificationHandler(
            controllerManager,
            hubContext,
            Logger.None);

        var notification = new AircraftDisconnected("hoppies-ybbb", "YBBB", "UAL123");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - no SignalR notification should be sent
        hubContext.Clients.DidNotReceive().Clients(Arg.Any<IReadOnlyList<string>>());
        await clientProxy.DidNotReceive().SendCoreAsync(
            Arg.Any<string>(),
            Arg.Any<object[]>(),
            Arg.Any<CancellationToken>());
    }
}
