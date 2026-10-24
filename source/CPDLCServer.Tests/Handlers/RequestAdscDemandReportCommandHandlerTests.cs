using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class RequestAdscDemandReportCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsRequestViaAcarsClientForStation()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var handler = new RequestAdscDemandReportCommandHandler(
            clientManager, new TestAdscContractRepository(), Substitute.For<IMediator>(), new TestClock(), Logger.None);

        // Act
        await handler.Handle(new RequestAdscDemandReportCommand("YBBB", "UAL123"), CancellationToken.None);

        // Assert
        var client = (TestAcarsClient) await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        Assert.Equal("UAL123", Assert.Single(client.RequestedAdscDemandReports));
        Assert.Empty(client.RequestedAdscContracts);
    }

    [Fact]
    public async Task Handle_WhenNoContractExists_AddsContractWithOnlyDemandReport()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var mediator = Substitute.For<IMediator>();
        var handler = new RequestAdscDemandReportCommandHandler(
            new TestClientManager(), contractRepository, mediator, new TestClock(), Logger.None);

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
            new TestClientManager(), contractRepository, mediator, new TestClock(), Logger.None);

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
