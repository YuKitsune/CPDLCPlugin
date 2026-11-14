using CPDLCServer.AdscContracts;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.AdscContracts;

public class CancelAdscContractCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenContractExists_SendsCancelAndRemovesContract()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10));
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new CancelAdscContractCommandHandler(acarsConnectionManager, contractRepository, mediator, new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new CancelAdscContractCommand("YBBB", "UAL123"), CancellationToken.None);

        // Assert
        var acarsConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        Assert.Equal("UAL123", Assert.Single(acarsConnection.CancelledAdscContracts));
        Assert.Empty(await contractRepository.All(CancellationToken.None));

        await mediator.Received(1).Publish(
            Arg.Is<AdscContractRemovedNotification>(n => n.Callsign == "UAL123" && n.StationId == "YBBB"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenContractNotFound_SendsNothing()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var mediator = Substitute.For<IMediator>();
        var handler = new CancelAdscContractCommandHandler(
            acarsConnectionManager, new TestAdscContractRepository(), mediator, new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new CancelAdscContractCommand("YBBB", "UAL123"), CancellationToken.None);

        // Assert
        var acarsConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        Assert.Empty(acarsConnection.CancelledAdscContracts);

        await mediator.DidNotReceive().Publish(
            Arg.Any<AdscContractRemovedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnlyRemovesContractForStation()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var ybbbContract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10));
        var ymmmContract = new AdscContract("UAL123", "YMMM", TimeSpan.FromMinutes(10));
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), ybbbContract, CancellationToken.None);
        await contractRepository.Add(new AdscContractKey("UAL123", "YMMM"), ymmmContract, CancellationToken.None);

        var handler = new CancelAdscContractCommandHandler(
            new TestAcarsConnectionManager(), contractRepository, Substitute.For<IMediator>(), new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new CancelAdscContractCommand("YBBB", "UAL123"), CancellationToken.None);

        // Assert
        var remaining = Assert.Single(await contractRepository.All(CancellationToken.None));
        Assert.Same(ymmmContract, remaining);
    }
}
