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
    public async Task Handle_WhenSelectedContractIsActive_KeepsSelectedContract()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10), DateTimeOffset.MinValue);
        contract.RequestSelectedContract(TimeSpan.FromMinutes(2));
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
        Assert.Equal(TimeSpan.FromMinutes(2), contract.SelectedReportingInterval);
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_WhenContractIsInactive_CancelsAndRemovesContract()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10), DateTimeOffset.MinValue);
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
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10), DateTimeOffset.MinValue);
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
    public async Task Handle_WhenContractIsInactiveAndAcarsCancelFails_RemovesContract()
    {
        // Arrange - TestAcarsConnectionManager has no ACARS connection for YSSY, so the cancel fails
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YSSY", TimeSpan.FromMinutes(10), DateTimeOffset.MinValue);
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
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10), DateTimeOffset.MinValue);
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
    public async Task Handle_WhenSelectedContractIsInactive_CancelsContract()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10), DateTimeOffset.MinValue);
        contract.RequestSelectedContract(TimeSpan.FromMinutes(2));
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
