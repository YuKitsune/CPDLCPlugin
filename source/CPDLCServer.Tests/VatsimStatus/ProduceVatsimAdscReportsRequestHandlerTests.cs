using CPDLCServer.AdscContracts;
using CPDLCServer.AdscContracts.Reports;
using CPDLCServer.Settings;
using CPDLCServer.VatsimStatus;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.VatsimStatus;

public class ProduceVatsimAdscReportsRequestHandlerTests
{
    static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(10);
    static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset LastUpdated = Now.AddSeconds(-10);
    static readonly TimeSpan ReportDelay = TimeSpan.FromMinutes(5);

    [Fact]
    public async Task Handle_WhenNoReport_PublishesVatsimReportAndAcceptsContract()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(DefaultInterval);

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, mediator);

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
    public async Task Handle_WhenContractIsNewerThanReportDelay_PublishesNothing()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository, Now - ReportDelay + TimeSpan.FromSeconds(1));
        contract.RequestDefaultContract(DefaultInterval);

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenContractIsReportDelayOld_PublishesVatsimReport()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository, Now - ReportDelay);
        contract.RequestDefaultContract(DefaultInterval);

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenVatsimReportIsDue_PublishesVatsimReport()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(DefaultInterval);

        ReceiveReport(contract, AdscReportSource.VATSIM, Now - DefaultInterval);

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, mediator);

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

        ReceiveReport(contract, AdscReportSource.VATSIM, Now.AddMinutes(-5));

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSourceIsHoppie_PublishesNothing()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(DefaultInterval);

        // The periodic report is due, but the report must come from Hoppie
        ReceiveReport(contract, AdscReportSource.Hoppie, Now - DefaultInterval * 2 - TimeSpan.FromSeconds(1));

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
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
        var handler = await CreateHandler(contractRepository, mediator);

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

        // The periodic report is not due
        ReceiveReport(contract, AdscReportSource.VATSIM, Now.AddMinutes(-1));
        contract.RequestDemandReport();

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, mediator);

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
        ReceiveReport(contract, AdscReportSource.Hoppie, Now.AddHours(-1));
        contract.RequestDemandReport();

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHoppieReportTimedOut_PublishesVatsimReport()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        ReceiveReport(contract, AdscReportSource.Hoppie, Now.AddHours(-1));
        contract.ReportTimedOut();
        contract.RequestDemandReport();

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
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
        var contract = new AdscContract("QFA1", "YBBB", TimeSpan.FromMinutes(10), DateTimeOffset.MinValue);
        await contractRepository.Add(new AdscContractKey("QFA1", "YBBB"), contract, CancellationToken.None);
        contract.RequestDefaultContract(DefaultInterval);

        var mediator = Substitute.For<IMediator>();
        var handler = await CreateHandler(contractRepository, mediator);

        // Act
        await handler.Handle(new ProduceVatsimAdscReportsRequest(), CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Publish(Arg.Any<AdscReportReceivedNotification>(), Arg.Any<CancellationToken>());
    }

    static async Task<ProduceVatsimAdscReportsRequestHandler> CreateHandler(
        IAdscContractRepository contractRepository,
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
            pilotRepository,
            new TestServerSettingsProvider(new ServerSettings
            {
                VatsimStatusEnabled = true,
                VatsimStatusAllowNonEquippedAircraft = allowNonEquippedAircraft,
                VatsimStatusReportDelayMinutes = (int)ReportDelay.TotalMinutes
            }),
            clock,
            mediator,
            Logger.None);
    }

    // The contract was made before the report delay, unless the test gives a different time
    static async Task<AdscContract> AddContract(IAdscContractRepository contractRepository, DateTimeOffset? created = null)
    {
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10), created ?? DateTimeOffset.MinValue);
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);
        return contract;
    }

    static void ReceiveReport(AdscContract contract, AdscReportSource source, DateTimeOffset timestamp)
    {
        contract.ReportReceived(new AdscReport(timestamp, -33.9461, 151.1772, 37000, source));
    }
}
