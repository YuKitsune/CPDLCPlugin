using CPDLCServer.AdscContracts;
using CPDLCServer.AdscContracts.Reports;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.AdscContracts.Reports;

public class RequestAdscDemandReportCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsRequestViaAcarsConnectionForStation()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var handler = new RequestAdscDemandReportCommandHandler(
            acarsConnectionManager, new TestAdscContractRepository(), Substitute.For<IMediator>(), new TestClock(), new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new RequestAdscDemandReportCommand("YBBB", "UAL123"), CancellationToken.None);

        // Assert
        var acarsConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        Assert.Equal("UAL123", Assert.Single(acarsConnection.RequestedAdscDemandReports));
        Assert.Empty(acarsConnection.RequestedAdscContracts);
    }

    [Fact]
    public async Task Handle_WhenNoContractExists_AddsContractWithOnlyDemandReport()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var mediator = Substitute.For<IMediator>();
        var handler = new RequestAdscDemandReportCommandHandler(
            new TestAcarsConnectionManager(), contractRepository, mediator, new TestClock(), new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new RequestAdscDemandReportCommand("YBBB", "UAL123"), CancellationToken.None);

        // Assert
        var contract = await contractRepository.Find(new AdscContractKey("UAL123", "YBBB"), CancellationToken.None);
        Assert.NotNull(contract);
        Assert.Null(contract.ReportingInterval);
        Assert.Null(contract.DefaultContractState);
        Assert.Null(contract.SelectedContractState);
        Assert.Equal(AdscContractState.Sent, contract.DemandContractState);

        await mediator.Received(1).Publish(
            Arg.Is<AdscContractChangedNotification>(n => n.Contract == contract),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPeriodicContractExists_KeepsPeriodicContractAndSendsDemandReport()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var existingContract = new AdscContract("UAL123", "YBBB");
        existingContract.RequestDefaultContract(TimeSpan.FromMinutes(10));
        existingContract.ReportReceived();
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), existingContract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new RequestAdscDemandReportCommandHandler(
            new TestAcarsConnectionManager(), contractRepository, mediator, new TestClock(), new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new RequestAdscDemandReportCommand("YBBB", "UAL123"), CancellationToken.None);

        // Assert
        var contract = Assert.Single(await contractRepository.All(CancellationToken.None));
        Assert.Same(existingContract, contract);
        Assert.Equal(TimeSpan.FromMinutes(10), contract.ReportingInterval);
        Assert.Equal(AdscContractState.Accepted, contract.DefaultContractState);
        Assert.Equal(AdscContractState.Sent, contract.DemandContractState);

        await mediator.Received(1).Publish(
            Arg.Is<AdscContractChangedNotification>(n => n.Contract == contract),
            Arg.Any<CancellationToken>());
    }
}
