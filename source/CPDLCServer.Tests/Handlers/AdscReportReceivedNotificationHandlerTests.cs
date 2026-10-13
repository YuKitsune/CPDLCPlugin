using CPDLCServer.Contracts;
using CPDLCServer.Handlers;
using CPDLCServer.Hubs;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class AdscReportReceivedNotificationHandlerTests
{
    [Fact]
    public async Task Handle_NotifiesAllConnectedControllers()
    {
        // Arrange
        var controllerRepository = new TestControllerRepository();
        await controllerRepository.Add(
            new ControllerInfo(Guid.NewGuid(), "ConnectionId-1", "YBBB", "BN-TSN_FSS", "1234567"),
            CancellationToken.None);
        await controllerRepository.Add(
            new ControllerInfo(Guid.NewGuid(), "ConnectionId-2", "YMMM", "ML-ASP_CTR", "7654321"),
            CancellationToken.None);

        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AdscReportReceivedNotificationHandler(controllerRepository, hubContext, Logger.None);

        var timestamp = new DateTimeOffset(2026, 10, 13, 11, 55, 0, TimeSpan.Zero);
        var received = timestamp.AddSeconds(40);
        var notification = new AdscReportReceivedNotification(
            "UAL123",
            "YBBB",
            new AdscReport(timestamp, -33.9461, 151.1772, 37000),
            received);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        hubContext.Clients.Received(1).Clients(
            Arg.Is<IReadOnlyList<string>>(ids =>
                ids.Count == 2 &&
                ids.Contains("ConnectionId-1") &&
                ids.Contains("ConnectionId-2")));

        var expectedDto = new AdscReportDto("UAL123", "YBBB", timestamp, -33.9461, 151.1772, 37000, received);
        await clientProxy.Received(1).SendCoreAsync(
            "AdscReportReceived",
            Arg.Is<object[]>(args => args.Length == 1 && expectedDto.Equals(args[0])),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotNotifyWhenNoControllersConnected()
    {
        // Arrange
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var handler = new AdscReportReceivedNotificationHandler(
            new TestControllerRepository(), hubContext, Logger.None);

        var notification = new AdscReportReceivedNotification(
            "UAL123",
            "YBBB",
            new AdscReport(DateTimeOffset.UtcNow, -33.9461, 151.1772, 37000),
            DateTimeOffset.UtcNow);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        hubContext.Clients.DidNotReceive().Clients(Arg.Any<IReadOnlyList<string>>());
    }
}
