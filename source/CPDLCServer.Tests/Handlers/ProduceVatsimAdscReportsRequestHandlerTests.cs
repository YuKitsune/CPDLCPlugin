using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Services;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class ProduceVatsimAdscReportsRequestHandlerTests
{
    static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(10);
    static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset LastUpdated = Now.AddSeconds(-10);

    [Fact]
    public async Task Handle_WhenNoReport_PublishesVatsimReportAndAcceptsContract()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(DefaultInterval);

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, new TestAdscReportRepository(), mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(
            Arg.Is<AdscReportReceivedNotification>(n =>
                n.Callsign == "UAL123" &&
                n.StationId == "YBBB" &&
                n.Report == new AdscReport(LastUpdated, -33.9461, 151.1772, 37000, AdscReportSource.VATSIM) &&
                n.Received == Now),
            Arg.Any<CancellationToken>());
        await mediator.Received(1).Publish(Arg.Any<AdscContractChangedNotification>(), Arg.Any<CancellationToken>());
        Assert.Equal(AdscContractState.Accepted, contract.DefaultContractState);
    }

    [Fact]
    public async Task Handle_WhenVatsimReportIsDue_PublishesVatsimReport()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(DefaultInterval);

        var reportRepository = new TestAdscReportRepository();
        await AddReport(reportRepository, AdscReportSource.VATSIM, Now - DefaultInterval);

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, reportRepository, mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenVatsimReportIsNotDue_PublishesNothing()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(DefaultInterval);

        var reportRepository = new TestAdscReportRepository();
        await AddReport(reportRepository, AdscReportSource.VATSIM, Now.AddMinutes(-5));

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, reportRepository, mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHoppieReportIsTwoIntervalsLate_PublishesNothing()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(DefaultInterval);

        var reportRepository = new TestAdscReportRepository();
        await AddReport(reportRepository, AdscReportSource.Hoppie, Now - DefaultInterval * 2);

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, reportRepository, mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHoppieReportIsMoreThanTwoIntervalsLate_PublishesVatsimReport()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(DefaultInterval);

        var reportRepository = new TestAdscReportRepository();
        await AddReport(reportRepository, AdscReportSource.Hoppie, Now - DefaultInterval * 2 - TimeSpan.FromSeconds(1));

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, reportRepository, mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenContractIsRejected_PublishesNothing()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(DefaultInterval);
        contract.Reject();

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, new TestAdscReportRepository(), mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDemandRequestIsSent_PublishesVatsimReportAndAcceptsDemand()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(DefaultInterval);
        contract.RequestDemandReport();

        // The periodic report is not due
        var reportRepository = new TestAdscReportRepository();
        await AddReport(reportRepository, AdscReportSource.VATSIM, Now.AddMinutes(-1));

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, reportRepository, mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
        Assert.Equal(AdscContractState.Accepted, contract.DemandContractState);
    }

    [Fact]
    public async Task Handle_WhenDemandOnlyContractHasHoppieReport_PublishesNothing()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDemandReport();

        var reportRepository = new TestAdscReportRepository();
        await AddReport(reportRepository, AdscReportSource.Hoppie, Now.AddHours(-1));

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, reportRepository, mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPilotIsNotEquipped_PublishesNothing()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(DefaultInterval);

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(
            contractRepository,
            new TestAdscReportRepository(),
            mediator,
            isAdscEquipped: false);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPilotIsNotEquippedAndNonEquippedAircraftAreAllowed_PublishesVatsimReport()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(DefaultInterval);

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(
            contractRepository,
            new TestAdscReportRepository(),
            mediator,
            isAdscEquipped: false,
            allowNonEquippedAircraft: true);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPilotIsNotInVatsimData_PublishesNothing()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("QFA1", "YBBB");
        await contractRepository.Add(new AdscContractKey("QFA1", "YBBB"), contract, CancellationToken.None);
        contract.RequestDefaultContract(DefaultInterval);

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, new TestAdscReportRepository(), mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
    }

    static async Task<ProduceVatsimAdscReportsRequestHandler> CreateHandler(
        IAdscContractRepository contractRepository,
        IAdscReportRepository reportRepository,
        IMediator mediator,
        bool isAdscEquipped = true,
        bool allowNonEquippedAircraft = false)
    {
        var pilotRepository = new InMemoryVatsimPilotRepository();
        await pilotRepository.Replace(
            [new VatsimPilot("UAL123", -33.9461, 151.1772, 37000, LastUpdated, isAdscEquipped)],
            CancellationToken.None);

        var clock = new TestClock();
        clock.SetUtcNow(Now);

        return new ProduceVatsimAdscReportsRequestHandler(
            contractRepository,
            reportRepository,
            pilotRepository,
            new VatsimStatusConfiguration { Enabled = true, AllowNonEquippedAircraft = allowNonEquippedAircraft },
            clock,
            mediator,
            Logger.None);
    }

    static async Task<AdscContract> AddContract(IAdscContractRepository contractRepository)
    {
        var contract = new AdscContract("UAL123", "YBBB");
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);
        return contract;
    }

    static async Task AddReport(IAdscReportRepository reportRepository, AdscReportSource source, DateTimeOffset received)
    {
        await reportRepository.Add(
            new AircraftAdscReport(
                "UAL123",
                "YBBB",
                new AdscReport(received.AddSeconds(-30), -33.9461, 151.1772, 37000, source),
                received),
            CancellationToken.None);
    }
}
