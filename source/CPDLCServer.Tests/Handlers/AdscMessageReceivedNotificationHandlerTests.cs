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
    static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(10);
    static readonly TimeSpan SelectedInterval = TimeSpan.FromMinutes(2);

    [Fact]
    public async Task Handle_Report_PublishesReport()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository, "YBBB");
        contract.RequestDefaultContract(DefaultInterval);

        var mediator = Substitute.For<IMediator>();
        var handler = new AdscMessageReceivedNotificationHandler(contractRepository, mediator, Logger.None);

        var report = new AdscReport(clock.UtcNow(), -33.9461, 151.1772, 37000, AdscReportSource.Hoppie);
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
        var contractRepository = new TestAdscContractRepository();
        var mediator = Substitute.For<IMediator>();
        var handler = new AdscMessageReceivedNotificationHandler(contractRepository, mediator, Logger.None);

        // Act
        await handler.Handle(CreateReport("YBBB"), CancellationToken.None);

        // Assert
        Assert.Empty(await contractRepository.All(CancellationToken.None));
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_Report_WhenContractIsWithOtherStation_IsIgnored()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository, "YMMM");
        contract.RequestDefaultContract(DefaultInterval);

        var mediator = Substitute.For<IMediator>();
        var handler = new AdscMessageReceivedNotificationHandler(contractRepository, mediator, Logger.None);

        // Act
        await handler.Handle(CreateReport("YBBB"), CancellationToken.None);

        // Assert
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_Report_WhenContractIsSent_AcceptsContract()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository, "YBBB");
        contract.RequestDefaultContract(DefaultInterval);

        var mediator = Substitute.For<IMediator>();
        var handler = new AdscMessageReceivedNotificationHandler(contractRepository, mediator, Logger.None);

        // Act
        await handler.Handle(CreateReport("YBBB"), CancellationToken.None);

        // Assert
        Assert.Equal(AdscContractState.Accepted, contract.DefaultContractState);

        await mediator.Received(1).Publish(
            Arg.Is<AdscContractChangedNotification>(n => n.Contract == contract),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Report_WhenContractIsAccepted_DoesNotPublishContractChange()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository, "YBBB");
        contract.RequestDefaultContract(DefaultInterval);
        contract.ReportReceived();

        var mediator = Substitute.For<IMediator>();
        var handler = new AdscMessageReceivedNotificationHandler(contractRepository, mediator, Logger.None);

        // Act
        await handler.Handle(CreateReport("YBBB"), CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Publish(
            Arg.Any<AdscContractChangedNotification>(),
            Arg.Any<CancellationToken>());
        await mediator.Received(1).Publish(
            Arg.Any<AdscReportReceivedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Report_WhenContractIsRejected_IsIgnored()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository, "YBBB");
        contract.RequestDefaultContract(DefaultInterval);
        contract.Reject();

        var mediator = Substitute.For<IMediator>();
        var handler = new AdscMessageReceivedNotificationHandler(contractRepository, mediator, Logger.None);

        // Act
        await handler.Handle(CreateReport("YBBB"), CancellationToken.None);

        // Assert
        Assert.Equal(AdscContractState.Rejected, contract.DefaultContractState);
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_Rejection_WhenSelectedContractIsInUse_RejectsSelectedContractAndRequestsDefaultContract()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository, "YBBB");
        contract.RequestSelectedContract(SelectedInterval, DateTimeOffset.UtcNow.AddHours(1));

        var mediator = Substitute.For<IMediator>();
        var handler = new AdscMessageReceivedNotificationHandler(contractRepository, mediator, Logger.None);

        // Act
        await handler.Handle(CreateRejection("YBBB"), CancellationToken.None);

        // Assert
        Assert.Same(contract, Assert.Single(await contractRepository.All(CancellationToken.None)));
        Assert.Equal(AdscContractState.Rejected, contract.SelectedContractState);

        await mediator.Received(1).Publish(
            Arg.Is<AdscContractChangedNotification>(n => n.Contract == contract),
            Arg.Any<CancellationToken>());
        await mediator.Received(1).Send(
            Arg.Is<RequestDefaultAdscContractCommand>(c => c.StationId == "YBBB" && c.Callsign == "UAL123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Rejection_WhenDefaultContractIsInUse_RejectsDefaultContractAndDoesNotRequestItAgain()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository, "YBBB");
        contract.RequestDefaultContract(DefaultInterval);

        var mediator = Substitute.For<IMediator>();
        var handler = new AdscMessageReceivedNotificationHandler(contractRepository, mediator, Logger.None);

        // Act
        await handler.Handle(CreateRejection("YBBB"), CancellationToken.None);

        // Assert
        Assert.Equal(AdscContractState.Rejected, contract.DefaultContractState);

        await mediator.Received(1).Publish(
            Arg.Is<AdscContractChangedNotification>(n => n.Contract == contract),
            Arg.Any<CancellationToken>());
        await mediator.DidNotReceive().Send(
            Arg.Any<RequestDefaultAdscContractCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Rejection_WhenNoContract_IsIgnored()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var handler = new AdscMessageReceivedNotificationHandler(
            new TestAdscContractRepository(), mediator, Logger.None);

        // Act
        await handler.Handle(CreateRejection("YBBB"), CancellationToken.None);

        // Assert
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_Report_LogsActivity()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository, "YBBB");
        contract.RequestDefaultContract(DefaultInterval);

        var handler = new AdscMessageReceivedNotificationHandler(
            contractRepository, Substitute.For<IMediator>(), Logger.None);
        var notification = CreateReport("YBBB");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Equal(notification.Message.Received, contract.LastActivity);
    }

    [Fact]
    public async Task Handle_Rejection_LogsActivity()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository, "YBBB");
        contract.RequestDefaultContract(DefaultInterval);

        var handler = new AdscMessageReceivedNotificationHandler(
            contractRepository, Substitute.For<IMediator>(), Logger.None);
        var notification = CreateRejection("YBBB");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Equal(notification.Message.Received, contract.LastActivity);
    }

    static async Task<AdscContract> AddContract(TestAdscContractRepository contractRepository, string stationId)
    {
        var contract = new AdscContract("UAL123", stationId);
        await contractRepository.Add(new AdscContractKey("UAL123", stationId), contract, CancellationToken.None);
        return contract;
    }

    static AdscMessageReceivedNotification CreateReport(string stationId)
    {
        var now = DateTimeOffset.UtcNow;
        return new AdscMessageReceivedNotification(
            $"hoppies-{stationId.ToLowerInvariant()}",
            stationId,
            new ReceivedAdscReport("UAL123", new AdscReport(now, -33.9461, 151.1772, 37000, AdscReportSource.Hoppie), now));
    }

    static AdscMessageReceivedNotification CreateRejection(string stationId)
    {
        return new AdscMessageReceivedNotification(
            $"hoppies-{stationId.ToLowerInvariant()}",
            stationId,
            new ReceivedAdscRejection("UAL123", DateTimeOffset.UtcNow));
    }
}
