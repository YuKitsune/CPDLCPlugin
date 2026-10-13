using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class CancelAdscContractCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenContractExists_SendsCancelAndRemovesContract()
    {
        // Arrange
        var clock = new TestClock();
        var clientManager = new TestClientManager();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(5), clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new CancelAdscContractCommandHandler(clientManager, contractRepository, mediator, Logger.None);

        // Act
        await handler.Handle(new CancelAdscContractCommand("YBBB", "UAL123"), CancellationToken.None);

        // Assert
        var client = (TestAcarsClient) await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        Assert.Equal("UAL123", Assert.Single(client.CancelledAdscContracts));
        Assert.Empty(await contractRepository.All(CancellationToken.None));

        await mediator.Received(1).Publish(
            Arg.Is<AdscContractRemovedNotification>(n => n.Callsign == "UAL123" && n.StationId == "YBBB"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenContractNotFound_SendsNothing()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var mediator = Substitute.For<IMediator>();
        var handler = new CancelAdscContractCommandHandler(
            clientManager, new TestAdscContractRepository(), mediator, Logger.None);

        // Act
        await handler.Handle(new CancelAdscContractCommand("YBBB", "UAL123"), CancellationToken.None);

        // Assert
        var client = (TestAcarsClient) await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        Assert.Empty(client.CancelledAdscContracts);

        await mediator.DidNotReceive().Publish(
            Arg.Any<AdscContractRemovedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnlyRemovesContractForStation()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var ybbbContract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(5), clock.UtcNow());
        var ymmmContract = new AdscContract("UAL123", "YMMM", TimeSpan.FromMinutes(5), clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), ybbbContract, CancellationToken.None);
        await contractRepository.Add(new AdscContractKey("UAL123", "YMMM"), ymmmContract, CancellationToken.None);

        var handler = new CancelAdscContractCommandHandler(
            new TestClientManager(), contractRepository, Substitute.For<IMediator>(), Logger.None);

        // Act
        await handler.Handle(new CancelAdscContractCommand("YBBB", "UAL123"), CancellationToken.None);

        // Assert
        var remaining = Assert.Single(await contractRepository.All(CancellationToken.None));
        Assert.Same(ymmmContract, remaining);
    }
}
