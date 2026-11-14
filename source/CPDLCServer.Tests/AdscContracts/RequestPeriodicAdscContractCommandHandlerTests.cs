using CPDLCServer.AdscContracts;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.AdscContracts;

public class RequestPeriodicAdscContractCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsDefaultIntervalViaAcarsConnectionForStation()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var handler = new RequestPeriodicAdscContractCommandHandler(
            acarsConnectionManager, new TestAdscContractRepository(), Substitute.For<IMediator>(), new TestClock(), new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new RequestPeriodicAdscContractCommand("YMMM", "UAL123", TimeSpan.FromMinutes(20)), CancellationToken.None);

        // Assert - TestAcarsConnectionManager sets the YMMM default to 20 minutes
        var ybbbConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        var ymmmConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ymmm", CancellationToken.None);
        Assert.Equal(TimeSpan.FromMinutes(20), ymmmConnection.RequestedAdscContracts["UAL123"]);
        Assert.Empty(ybbbConnection.RequestedAdscContracts);
    }

    [Fact]
    public async Task Handle_WithDefaultInterval_WhenNoContractExists_AddsDefaultContract()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var mediator = Substitute.For<IMediator>();
        var handler = new RequestPeriodicAdscContractCommandHandler(
            new TestAcarsConnectionManager(), contractRepository, mediator, new TestClock(), new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new RequestPeriodicAdscContractCommand("YBBB", "UAL123", TimeSpan.FromMinutes(10)), CancellationToken.None);

        // Assert - TestAcarsConnectionManager sets the YBBB default to 10 minutes
        var contract = await contractRepository.Find(new AdscContractKey("UAL123", "YBBB"), CancellationToken.None);
        Assert.NotNull(contract);
        Assert.Equal(TimeSpan.FromMinutes(10), contract.ReportingInterval);
        Assert.Equal(AdscContractState.Sent, contract.DefaultContractState);
        Assert.Null(contract.SelectedContractState);
        Assert.Null(contract.SelectedContractEnds);

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
        var handler = new RequestPeriodicAdscContractCommandHandler(
            new TestAcarsConnectionManager(), contractRepository, Substitute.For<IMediator>(), clock, new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new RequestPeriodicAdscContractCommand("YBBB", "UAL123", TimeSpan.FromMinutes(10)), CancellationToken.None);

        // Assert
        var contract = await contractRepository.Find(new AdscContractKey("UAL123", "YBBB"), CancellationToken.None);
        Assert.NotNull(contract);
        Assert.Equal(clock.UtcNow(), contract.LastActivity);
    }

    [Fact]
    public async Task Handle_WithDefaultInterval_WhenSelectedContractIsInUse_EndsSelectedContract()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var existingContract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10));
        existingContract.RequestSelectedContract(TimeSpan.FromMinutes(2), DateTimeOffset.UtcNow.AddHours(1));
        existingContract.ReportReceived();
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), existingContract, CancellationToken.None);

        var handler = new RequestPeriodicAdscContractCommandHandler(
            new TestAcarsConnectionManager(), contractRepository, Substitute.For<IMediator>(), new TestClock(), new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new RequestPeriodicAdscContractCommand("YBBB", "UAL123", TimeSpan.FromMinutes(10)), CancellationToken.None);

        // Assert
        var contract = Assert.Single(await contractRepository.All(CancellationToken.None));
        Assert.Same(existingContract, contract);
        Assert.Equal(TimeSpan.FromMinutes(10), contract.ReportingInterval);
        Assert.Equal(AdscContractState.Sent, contract.DefaultContractState);
        Assert.Null(contract.SelectedContractState);
        Assert.Null(contract.SelectedContractEnds);
    }

    [Fact]
    public async Task Handle_WithOtherInterval_SendsRequestViaAcarsConnectionForStation()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var handler = new RequestPeriodicAdscContractCommandHandler(
            acarsConnectionManager, new TestAdscContractRepository(), Substitute.For<IMediator>(), new TestClock(), new TestUnitOfWork(), Logger.None);

        var command = new RequestPeriodicAdscContractCommand("YBBB", "UAL123", TimeSpan.FromMinutes(5));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var ybbbConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        var ymmmConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ymmm", CancellationToken.None);
        Assert.Equal(TimeSpan.FromMinutes(5), ybbbConnection.RequestedAdscContracts["UAL123"]);
        Assert.Empty(ymmmConnection.RequestedAdscContracts);
    }

    [Fact]
    public async Task Handle_WithOtherInterval_WhenNoContractExists_AddsSelectedContract()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var mediator = Substitute.For<IMediator>();
        var handler = new RequestPeriodicAdscContractCommandHandler(
            new TestAcarsConnectionManager(), contractRepository, mediator, clock, new TestUnitOfWork(), Logger.None);

        var command = new RequestPeriodicAdscContractCommand("YBBB", "UAL123", TimeSpan.FromMinutes(5));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var contract = await contractRepository.Find(new AdscContractKey("UAL123", "YBBB"), CancellationToken.None);
        Assert.NotNull(contract);
        Assert.Equal(TimeSpan.FromMinutes(5), contract.ReportingInterval);
        Assert.Equal(TimeSpan.FromMinutes(5), contract.SelectedReportingInterval);
        Assert.Equal(AdscContractState.Sent, contract.SelectedContractState);
        Assert.Null(contract.DefaultContractState);
        Assert.Equal(TimeSpan.FromMinutes(10), contract.DefaultReportingInterval);

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
        var handler = new RequestPeriodicAdscContractCommandHandler(
            acarsConnectionManager, contractRepository, Substitute.For<IMediator>(), clock, new TestUnitOfWork(), Logger.None);

        var command = new RequestPeriodicAdscContractCommand("YBBB", "UAL123", TimeSpan.FromMinutes(5));

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
        var existingContract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10));
        existingContract.RequestDefaultContract(TimeSpan.FromMinutes(10));
        existingContract.ReportReceived();
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), existingContract, CancellationToken.None);

        var handler = new RequestPeriodicAdscContractCommandHandler(
            new TestAcarsConnectionManager(), contractRepository, Substitute.For<IMediator>(), new TestClock(), new TestUnitOfWork(), Logger.None);

        var command = new RequestPeriodicAdscContractCommand("YBBB", "UAL123", TimeSpan.FromMinutes(2));

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
        var handler = new RequestPeriodicAdscContractCommandHandler(
            acarsConnectionManager, contractRepository, Substitute.For<IMediator>(), new TestClock(), new TestUnitOfWork(), Logger.None);

        var command = new RequestPeriodicAdscContractCommand("YBBB", "UAL123", TimeSpan.FromSeconds(59));

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
        var handler = new RequestPeriodicAdscContractCommandHandler(
            acarsConnectionManager, new TestAdscContractRepository(), Substitute.For<IMediator>(), new TestClock(), new TestUnitOfWork(), Logger.None);

        var command = new RequestPeriodicAdscContractCommand(
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
