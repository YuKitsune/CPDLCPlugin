using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class EndExpiredAdscContractsRequestHandlerTests
{
    static readonly TimeSpan ContractTimeout = TimeSpan.FromMinutes(120);

    [Fact]
    public async Task Handle_WhenSelectedContractHasEnded_RequestsDefaultContract()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestSelectedContract(TimeSpan.FromMinutes(2), clock.UtcNow());
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            new TestClientManager(), contractRepository, clock, mediator, Logger.None);

        // Act
        await handler.Handle(new EndExpiredAdscContractsRequest(ContractTimeout), CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<RequestDefaultAdscContractCommand>(c => c.StationId == "YBBB" && c.Callsign == "UAL123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSelectedContractHasNotEnded_DoesNothing()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestSelectedContract(TimeSpan.FromMinutes(2), clock.UtcNow().AddSeconds(1));
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            new TestClientManager(), contractRepository, clock, mediator, Logger.None);

        // Act
        await handler.Handle(new EndExpiredAdscContractsRequest(ContractTimeout), CancellationToken.None);

        // Assert
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_WhenNoSelectedContract_DoesNothing()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestDefaultContract(TimeSpan.FromMinutes(10));
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            new TestClientManager(), contractRepository, clock, mediator, Logger.None);

        // Act
        await handler.Handle(new EndExpiredAdscContractsRequest(ContractTimeout), CancellationToken.None);

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
            var contract = new AdscContract(callsign, "YBBB");
            contract.RequestSelectedContract(TimeSpan.FromMinutes(2), clock.UtcNow());
            contract.LogActivity(clock.UtcNow());
            await contractRepository.Add(new AdscContractKey(callsign, "YBBB"), contract, CancellationToken.None);
        }

        var mediator = Substitute.For<IMediator>();
        mediator
            .Send(Arg.Is<RequestDefaultAdscContractCommand>(c => c.Callsign == "UAL123"), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("Hoppie is down")));

        var handler = new EndExpiredAdscContractsRequestHandler(
            new TestClientManager(), contractRepository, clock, mediator, Logger.None);

        // Act
        await handler.Handle(new EndExpiredAdscContractsRequest(ContractTimeout), CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<RequestDefaultAdscContractCommand>(c => c.Callsign == "QFA1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenContractIsInactive_CancelsAndRemovesContract()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestDefaultContract(TimeSpan.FromMinutes(10));
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var clientManager = new TestClientManager();
        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            clientManager, contractRepository, clock, mediator, Logger.None);

        // Act
        clock.SetUtcNow(clock.UtcNow() + ContractTimeout + TimeSpan.FromMinutes(1));
        await handler.Handle(new EndExpiredAdscContractsRequest(ContractTimeout), CancellationToken.None);

        // Assert
        var client = (TestAcarsClient) await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        Assert.Equal(["UAL123"], client.CancelledAdscContracts);
        Assert.Empty(await contractRepository.All(CancellationToken.None));
        await mediator.Received(1).Publish(
            Arg.Is<AdscContractRemovedNotification>(n => n.Callsign == "UAL123" && n.StationId == "YBBB"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenContractIsInactiveAndAcarsCancelFails_RemovesContract()
    {
        // Arrange - TestClientManager has no ACARS client for YSSY, so the cancel fails
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YSSY");
        contract.RequestDefaultContract(TimeSpan.FromMinutes(10));
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YSSY"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            new TestClientManager(), contractRepository, clock, mediator, Logger.None);

        // Act
        clock.SetUtcNow(clock.UtcNow() + ContractTimeout + TimeSpan.FromMinutes(1));
        await handler.Handle(new EndExpiredAdscContractsRequest(ContractTimeout), CancellationToken.None);

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
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestDefaultContract(TimeSpan.FromMinutes(10));
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            new TestClientManager(), contractRepository, clock, mediator, Logger.None);

        // Act
        clock.SetUtcNow(clock.UtcNow() + ContractTimeout - TimeSpan.FromMinutes(1));
        await handler.Handle(new EndExpiredAdscContractsRequest(ContractTimeout), CancellationToken.None);

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
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestSelectedContract(TimeSpan.FromMinutes(2), clock.UtcNow().AddMinutes(60));
        contract.LogActivity(clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(
            new TestClientManager(), contractRepository, clock, mediator, Logger.None);

        // Act
        clock.SetUtcNow(clock.UtcNow() + ContractTimeout + TimeSpan.FromMinutes(1));
        await handler.Handle(new EndExpiredAdscContractsRequest(ContractTimeout), CancellationToken.None);

        // Assert
        Assert.Empty(await contractRepository.All(CancellationToken.None));
        await mediator.DidNotReceive().Send(
            Arg.Any<RequestDefaultAdscContractCommand>(),
            Arg.Any<CancellationToken>());
    }
}
