using CPDLCServer.AdscContracts;
using CPDLCServer.Infrastructure;
using CPDLCServer.Settings;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.AdscContracts;

public class EndExpiredAdscContractsRequestHandlerTests
{
    static readonly TimeSpan ContractTimeout = TimeSpan.FromMinutes(120);
    static readonly TestServerSettingsProvider Settings = new(new ServerSettings { AdscContractTimeoutMinutes = 120 });

    // The server has been online for longer than any timeout
    static readonly ServerStartTime StartedLongAgo = new(DateTimeOffset.MinValue);

    [Fact]
    public async Task Handle_WhenSelectedContractHasEnded_RequestsDefaultContract()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10));
        contract.RequestSelectedContract(TimeSpan.FromMinutes(2), clock.UtcNow());
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            new TestAcarsConnectionManager(), contractRepository, Settings, clock, StartedLongAgo, mediator, new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new EndExpiredAdscContractsRequest(), CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<RequestPeriodicAdscContractCommand>(c => c.StationId == "YBBB" && c.Callsign == "UAL123" && c.ReportingInterval == TimeSpan.FromMinutes(10)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSelectedContractHasNotEnded_DoesNothing()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10));
        contract.RequestSelectedContract(TimeSpan.FromMinutes(2), clock.UtcNow().AddSeconds(1));
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            new TestAcarsConnectionManager(), contractRepository, Settings, clock, StartedLongAgo, mediator, new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new EndExpiredAdscContractsRequest(), CancellationToken.None);

        // Assert
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_WhenNoSelectedContract_DoesNothing()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10));
        contract.RequestDefaultContract(TimeSpan.FromMinutes(10));
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            new TestAcarsConnectionManager(), contractRepository, Settings, clock, StartedLongAgo, mediator, new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new EndExpiredAdscContractsRequest(), CancellationToken.None);

        // Assert
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_WhenRequestFails_ContinuesWithOtherContracts()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        foreach (var callsign in new[] { "UAL123", "QFA1" })
        {
            var contract = new AdscContract(callsign, "YBBB", TimeSpan.FromMinutes(10));
            contract.RequestSelectedContract(TimeSpan.FromMinutes(2), clock.UtcNow());
            contract.LogActivity(clock.UtcNow());
            await contractRepository.Add(new AdscContractKey(callsign, "YBBB"), contract, CancellationToken.None);
        }

        var mediator = Substitute.For<IMediator>();
        mediator
            .Send(Arg.Is<RequestPeriodicAdscContractCommand>(c => c.Callsign == "UAL123"), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("Hoppie is down")));

        var handler = new EndExpiredAdscContractsRequestHandler(
            new TestAcarsConnectionManager(), contractRepository, Settings, clock, StartedLongAgo, mediator, new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new EndExpiredAdscContractsRequest(), CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<RequestPeriodicAdscContractCommand>(c => c.Callsign == "QFA1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenContractIsInactive_CancelsAndRemovesContract()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10));
        contract.RequestDefaultContract(TimeSpan.FromMinutes(10));
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var acarsConnectionManager = new TestAcarsConnectionManager();
        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            acarsConnectionManager, contractRepository, Settings, clock, StartedLongAgo, mediator, new TestUnitOfWork(), Logger.None);

        // Act
        clock.SetUtcNow(clock.UtcNow() + ContractTimeout + TimeSpan.FromMinutes(1));
        await handler.Handle(new EndExpiredAdscContractsRequest(), CancellationToken.None);

        // Assert
        var acarsConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        Assert.Equal(["UAL123"], acarsConnection.CancelledAdscContracts);
        Assert.Empty(await contractRepository.All(CancellationToken.None));
        await mediator.Received(1).Publish(
            Arg.Is<AdscContractRemovedNotification>(n => n.Callsign == "UAL123" && n.StationId == "YBBB"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenServerOnlineForLessThanTimeout_KeepsInactiveContract()
    {
        // Arrange - the last activity was long before the server started
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10));
        contract.RequestDefaultContract(TimeSpan.FromMinutes(10));
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var serverStarted = clock.UtcNow() + ContractTimeout;
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            acarsConnectionManager, contractRepository, Settings, clock, new ServerStartTime(serverStarted), mediator, new TestUnitOfWork(), Logger.None);

        // Act
        clock.SetUtcNow(serverStarted + ContractTimeout);
        await handler.Handle(new EndExpiredAdscContractsRequest(), CancellationToken.None);

        // Assert
        var acarsConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        Assert.Empty(acarsConnection.CancelledAdscContracts);
        Assert.Equal([contract], await contractRepository.All(CancellationToken.None));
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_WhenServerOnlineForLessThanTimeout_StillEndsSelectedContract()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10));
        contract.RequestSelectedContract(TimeSpan.FromMinutes(2), clock.UtcNow());
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            new TestAcarsConnectionManager(), contractRepository, Settings, clock, new ServerStartTime(clock.UtcNow()), mediator, new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new EndExpiredAdscContractsRequest(), CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<RequestPeriodicAdscContractCommand>(c => c.StationId == "YBBB" && c.Callsign == "UAL123" && c.ReportingInterval == TimeSpan.FromMinutes(10)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenContractIsInactiveAndAcarsCancelFails_RemovesContract()
    {
        // Arrange - TestAcarsConnectionManager has no ACARS connection for YSSY, so the cancel fails
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YSSY", TimeSpan.FromMinutes(10));
        contract.RequestDefaultContract(TimeSpan.FromMinutes(10));
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YSSY"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            new TestAcarsConnectionManager(), contractRepository, Settings, clock, StartedLongAgo, mediator, new TestUnitOfWork(), Logger.None);

        // Act
        clock.SetUtcNow(clock.UtcNow() + ContractTimeout + TimeSpan.FromMinutes(1));
        await handler.Handle(new EndExpiredAdscContractsRequest(), CancellationToken.None);

        // Assert
        Assert.Empty(await contractRepository.All(CancellationToken.None));
        await mediator.Received(1).Publish(
            Arg.Is<AdscContractRemovedNotification>(n => n.Callsign == "UAL123" && n.StationId == "YSSY"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenContractHasRecentActivity_KeepsContract()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10));
        contract.RequestDefaultContract(TimeSpan.FromMinutes(10));
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            new TestAcarsConnectionManager(), contractRepository, Settings, clock, StartedLongAgo, mediator, new TestUnitOfWork(), Logger.None);

        // Act
        clock.SetUtcNow(clock.UtcNow() + ContractTimeout - TimeSpan.FromMinutes(1));
        await handler.Handle(new EndExpiredAdscContractsRequest(), CancellationToken.None);

        // Assert
        Assert.Same(contract, Assert.Single(await contractRepository.All(CancellationToken.None)));
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_WhenSelectedContractIsInactive_CancelsInsteadOfRequestingDefault()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10));
        contract.RequestSelectedContract(TimeSpan.FromMinutes(2), clock.UtcNow().AddMinutes(60));
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            new TestAcarsConnectionManager(), contractRepository, Settings, clock, StartedLongAgo, mediator, new TestUnitOfWork(), Logger.None);

        // Act
        clock.SetUtcNow(clock.UtcNow() + ContractTimeout + TimeSpan.FromMinutes(1));
        await handler.Handle(new EndExpiredAdscContractsRequest(), CancellationToken.None);

        // Assert
        Assert.Empty(await contractRepository.All(CancellationToken.None));
        await mediator.DidNotReceive().Send(
            Arg.Any<RequestPeriodicAdscContractCommand>(),
            Arg.Any<CancellationToken>());
    }
}
