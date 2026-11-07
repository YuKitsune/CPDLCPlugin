using CPDLCServer.AdscContracts;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.AdscContracts;

public class RequestSelectedAdscContractCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsRequestViaAcarsConnectionForStation()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var handler = new RequestSelectedAdscContractCommandHandler(
            acarsConnectionManager, new TestAdscContractRepository(), Substitute.For<IMediator>(), new TestClock(), new TestUnitOfWork(), Logger.None);

        var command = new RequestSelectedAdscContractCommand("YBBB", "UAL123", TimeSpan.FromMinutes(5));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var ybbbConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        var ymmmConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ymmm", CancellationToken.None);
        Assert.Equal(TimeSpan.FromMinutes(5), ybbbConnection.RequestedAdscContracts["UAL123"]);
        Assert.Empty(ymmmConnection.RequestedAdscContracts);
    }

    [Fact]
    public async Task Handle_WhenNoContractExists_AddsSelectedContract()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var mediator = Substitute.For<IMediator>();
        var handler = new RequestSelectedAdscContractCommandHandler(
            new TestAcarsConnectionManager(), contractRepository, mediator, clock, new TestUnitOfWork(), Logger.None);

        var command = new RequestSelectedAdscContractCommand("YBBB", "UAL123", TimeSpan.FromMinutes(5));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var contract = await contractRepository.Find(new AdscContractKey("UAL123", "YBBB"), CancellationToken.None);
        Assert.NotNull(contract);
        Assert.Equal(TimeSpan.FromMinutes(5), contract.ReportingInterval);
        Assert.Equal(TimeSpan.FromMinutes(5), contract.SelectedReportingInterval);
        Assert.Equal(AdscContractState.Sent, contract.SelectedContractState);
        Assert.Null(contract.DefaultContractState);

        await mediator.Received(1).Publish(
            Arg.Is<AdscContractChangedNotification>(n => n.Contract == contract),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetsEndTimeFromRequestTime()
    {
        // Arrange
        var clock = new TestClock();
        var acarsConnectionManager = new TestAcarsConnectionManager();
        acarsConnectionManager.AddConfiguration(TestAcarsConnectionManager.CreateConfiguration("YBBB", 10, selectedAdscContractDuration: 30));

        var contractRepository = new TestAdscContractRepository();
        var handler = new RequestSelectedAdscContractCommandHandler(
            acarsConnectionManager, contractRepository, Substitute.For<IMediator>(), clock, new TestUnitOfWork(), Logger.None);

        var command = new RequestSelectedAdscContractCommand("YBBB", "UAL123", TimeSpan.FromMinutes(5));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var contract = await contractRepository.Find(new AdscContractKey("UAL123", "YBBB"), CancellationToken.None);
        Assert.NotNull(contract);
        Assert.Equal(clock.UtcNow().AddMinutes(30), contract.SelectedContractEnds);
    }

    [Fact]
    public async Task Handle_WhenDefaultContractExists_KeepsDefaultContractUntilAccepted()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var existingContract = new AdscContract("UAL123", "YBBB");
        existingContract.RequestDefaultContract(TimeSpan.FromMinutes(10));
        existingContract.ReportReceived();
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), existingContract, CancellationToken.None);

        var handler = new RequestSelectedAdscContractCommandHandler(
            new TestAcarsConnectionManager(), contractRepository, Substitute.For<IMediator>(), new TestClock(), new TestUnitOfWork(), Logger.None);

        var command = new RequestSelectedAdscContractCommand("YBBB", "UAL123", TimeSpan.FromMinutes(2));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var contract = Assert.Single(await contractRepository.All(CancellationToken.None));
        Assert.Same(existingContract, contract);
        Assert.Equal(TimeSpan.FromMinutes(2), contract.ReportingInterval);
        Assert.Equal(AdscContractState.Sent, contract.SelectedContractState);
        Assert.Equal(AdscContractState.Accepted, contract.DefaultContractState);
    }

    [Fact]
    public async Task Handle_WhenIntervalIsLessThanMinimum_ThrowsAndSendsNothing()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var contractRepository = new TestAdscContractRepository();
        var handler = new RequestSelectedAdscContractCommandHandler(
            acarsConnectionManager, contractRepository, Substitute.For<IMediator>(), new TestClock(), new TestUnitOfWork(), Logger.None);

        var command = new RequestSelectedAdscContractCommand("YBBB", "UAL123", TimeSpan.FromSeconds(59));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => handler.Handle(command, CancellationToken.None));

        var acarsConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        Assert.Empty(acarsConnection.RequestedAdscContracts);
        Assert.Empty(await contractRepository.All(CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenIntervalIsMinimum_SendsRequest()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var handler = new RequestSelectedAdscContractCommandHandler(
            acarsConnectionManager, new TestAdscContractRepository(), Substitute.For<IMediator>(), new TestClock(), new TestUnitOfWork(), Logger.None);

        var command = new RequestSelectedAdscContractCommand(
            "YBBB",
            "UAL123",
            AdscContract.MinimumReportingInterval);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var acarsConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        Assert.Single(acarsConnection.RequestedAdscContracts);
    }
}
