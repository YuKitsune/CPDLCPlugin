using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class RequestPeriodicAdscContractCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsRequestViaAcarsClientForStation()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var handler = new RequestPeriodicAdscContractCommandHandler(
            clientManager, new TestAdscContractRepository(), Substitute.For<IMediator>(), new TestClock(), Logger.None);

        var command = new RequestPeriodicAdscContractCommand("YBBB", "UAL123", TimeSpan.FromMinutes(5));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var ybbbClient = (TestAcarsClient) await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        var ymmmClient = (TestAcarsClient) await clientManager.GetAcarsClient("hoppies-ymmm", CancellationToken.None);
        Assert.Equal(TimeSpan.FromMinutes(5), ybbbClient.RequestedAdscContracts["UAL123"]);
        Assert.Empty(ymmmClient.RequestedAdscContracts);
    }

    [Fact]
    public async Task Handle_WhenNoContractExists_AddsContract()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var mediator = Substitute.For<IMediator>();
        var handler = new RequestPeriodicAdscContractCommandHandler(
            new TestClientManager(), contractRepository, mediator, clock, Logger.None);

        var command = new RequestPeriodicAdscContractCommand("YBBB", "UAL123", TimeSpan.FromMinutes(5));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var contract = await contractRepository.Find(new AdscContractKey("UAL123", "YBBB"), CancellationToken.None);
        Assert.NotNull(contract);
        Assert.Equal(TimeSpan.FromMinutes(5), contract.ReportingInterval);
        Assert.Equal(clock.UtcNow(), contract.Requested);

        await mediator.Received(1).Publish(
            Arg.Is<AdscContractChangedNotification>(n => n.Contract == contract),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenContractExists_ChangesReportingInterval()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var existingContract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10), clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), existingContract, CancellationToken.None);

        var handler = new RequestPeriodicAdscContractCommandHandler(
            new TestClientManager(), contractRepository, Substitute.For<IMediator>(), clock, Logger.None);

        clock.SetUtcNow(clock.UtcNow().AddMinutes(1));
        var command = new RequestPeriodicAdscContractCommand("YBBB", "UAL123", TimeSpan.FromMinutes(2));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var contracts = await contractRepository.All(CancellationToken.None);
        var contract = Assert.Single(contracts);
        Assert.Same(existingContract, contract);
        Assert.Equal(TimeSpan.FromMinutes(2), contract.ReportingInterval);
        Assert.Equal(clock.UtcNow(), contract.Requested);
    }

    [Fact]
    public async Task Handle_WhenIntervalIsLessThanMinimum_ThrowsAndSendsNothing()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var contractRepository = new TestAdscContractRepository();
        var handler = new RequestPeriodicAdscContractCommandHandler(
            clientManager, contractRepository, Substitute.For<IMediator>(), new TestClock(), Logger.None);

        var command = new RequestPeriodicAdscContractCommand("YBBB", "UAL123", TimeSpan.FromSeconds(59));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => handler.Handle(command, CancellationToken.None));

        var client = (TestAcarsClient) await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        Assert.Empty(client.RequestedAdscContracts);
        Assert.Empty(await contractRepository.All(CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenIntervalIsMinimum_SendsRequest()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var handler = new RequestPeriodicAdscContractCommandHandler(
            clientManager, new TestAdscContractRepository(), Substitute.For<IMediator>(), new TestClock(), Logger.None);

        var command = new RequestPeriodicAdscContractCommand(
            "YBBB",
            "UAL123",
            AdscContract.MinimumReportingInterval);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var client = (TestAcarsClient) await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        Assert.Single(client.RequestedAdscContracts);
    }
}
