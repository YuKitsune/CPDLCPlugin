using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class AdscMessageReceivedNotificationHandlerTests
{
    [Fact]
    public async Task Handle_Report_PublishesReport()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(5), clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new AdscMessageReceivedNotificationHandler(contractRepository, mediator, Logger.None);

        var report = new AdscReport(clock.UtcNow(), -33.9461, 151.1772, 37000);
        var received = clock.UtcNow().AddSeconds(30);
        var notification = new AdscMessageReceivedNotification(
            "hoppies-ybbb",
            "YBBB",
            new ReceivedAdscReport("UAL123", report, received));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(
            Arg.Is<AdscReportReceivedNotification>(n =>
                n.Callsign == "UAL123" &&
                n.StationId == "YBBB" &&
                n.Report == report &&
                n.Received == received),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Report_WhenNoContract_IsIgnored()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var mediator = Substitute.For<IMediator>();
        var handler = new AdscMessageReceivedNotificationHandler(contractRepository, mediator, Logger.None);

        var report = new AdscReport(clock.UtcNow(), -33.9461, 151.1772, 37000);
        var notification = new AdscMessageReceivedNotification(
            "hoppies-ybbb",
            "YBBB",
            new ReceivedAdscReport("UAL123", report, clock.UtcNow()));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Empty(await contractRepository.All(CancellationToken.None));
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_Report_WhenContractIsWithOtherStation_IsIgnored()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YMMM", TimeSpan.FromMinutes(5), clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YMMM"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new AdscMessageReceivedNotificationHandler(contractRepository, mediator, Logger.None);

        var report = new AdscReport(clock.UtcNow(), -33.9461, 151.1772, 37000);
        var notification = new AdscMessageReceivedNotification(
            "hoppies-ybbb",
            "YBBB",
            new ReceivedAdscReport("UAL123", report, clock.UtcNow()));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_Rejection_RemovesContract()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(5), clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new AdscMessageReceivedNotificationHandler(contractRepository, mediator, Logger.None);

        var notification = new AdscMessageReceivedNotification(
            "hoppies-ybbb",
            "YBBB",
            new ReceivedAdscRejection("UAL123", clock.UtcNow()));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Empty(await contractRepository.All(CancellationToken.None));

        await mediator.Received(1).Publish(
            Arg.Is<AdscContractRemovedNotification>(n => n.Callsign == "UAL123" && n.StationId == "YBBB"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Rejection_WhenNoContract_IsIgnored()
    {
        // Arrange
        var clock = new TestClock();
        var mediator = Substitute.For<IMediator>();
        var handler = new AdscMessageReceivedNotificationHandler(
            new TestAdscContractRepository(), mediator, Logger.None);

        var notification = new AdscMessageReceivedNotification(
            "hoppies-ybbb",
            "YBBB",
            new ReceivedAdscRejection("UAL123", clock.UtcNow()));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Empty(mediator.ReceivedCalls());
    }
}
