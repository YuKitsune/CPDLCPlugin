using CPDLCServer.AdscContracts;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.AdscContracts;

public class RequestDefaultAdscContractCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsDefaultIntervalViaAcarsConnectionForStation()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var handler = new RequestDefaultAdscContractCommandHandler(
            acarsConnectionManager, new TestAdscContractRepository(), Substitute.For<IMediator>(), new TestClock(), new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new RequestDefaultAdscContractCommand("YMMM", "UAL123"), CancellationToken.None);

        // Assert - TestAcarsConnectionManager sets the YMMM default to 20 minutes
        var ybbbConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        var ymmmConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ymmm", CancellationToken.None);
        Assert.Equal(TimeSpan.FromMinutes(20), ymmmConnection.RequestedAdscContracts["UAL123"]);
        Assert.Empty(ybbbConnection.RequestedAdscContracts);
    }

    [Fact]
    public async Task Handle_WhenNoContractExists_AddsDefaultContract()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var mediator = Substitute.For<IMediator>();
        var handler = new RequestDefaultAdscContractCommandHandler(
            new TestAcarsConnectionManager(), contractRepository, mediator, new TestClock(), new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new RequestDefaultAdscContractCommand("YBBB", "UAL123"), CancellationToken.None);

        // Assert - TestAcarsConnectionManager sets the YBBB default to 10 minutes
        var contract = await contractRepository.Find(new AdscContractKey("UAL123", "YBBB"), CancellationToken.None);
        Assert.NotNull(contract);
        Assert.Equal(TimeSpan.FromMinutes(10), contract.ReportingInterval);
        Assert.Equal(AdscContractState.Sent, contract.DefaultContractState);

        await mediator.Received(1).Publish(
            Arg.Is<AdscContractChangedNotification>(n => n.Contract == contract),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LogsActivity()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var handler = new RequestDefaultAdscContractCommandHandler(
            new TestAcarsConnectionManager(), contractRepository, Substitute.For<IMediator>(), clock, new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new RequestDefaultAdscContractCommand("YBBB", "UAL123"), CancellationToken.None);

        // Assert
        var contract = await contractRepository.Find(new AdscContractKey("UAL123", "YBBB"), CancellationToken.None);
        Assert.NotNull(contract);
        Assert.Equal(clock.UtcNow(), contract.LastActivity);
    }

    [Fact]
    public async Task Handle_WhenSelectedContractIsInUse_EndsSelectedContract()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var existingContract = new AdscContract("UAL123", "YBBB");
        existingContract.RequestSelectedContract(TimeSpan.FromMinutes(2), DateTimeOffset.UtcNow.AddHours(1));
        existingContract.ReportReceived();
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), existingContract, CancellationToken.None);

        var handler = new RequestDefaultAdscContractCommandHandler(
            new TestAcarsConnectionManager(), contractRepository, Substitute.For<IMediator>(), new TestClock(), new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new RequestDefaultAdscContractCommand("YBBB", "UAL123"), CancellationToken.None);

        // Assert
        var contract = Assert.Single(await contractRepository.All(CancellationToken.None));
        Assert.Same(existingContract, contract);
        Assert.Equal(TimeSpan.FromMinutes(10), contract.ReportingInterval);
        Assert.Equal(AdscContractState.Sent, contract.DefaultContractState);
        Assert.Null(contract.SelectedContractState);
        Assert.Null(contract.SelectedContractEnds);
    }
}
