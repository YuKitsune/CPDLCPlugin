using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class RequestSelectedAdscContractCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsRequestViaAcarsClientForStation()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var handler = new RequestSelectedAdscContractCommandHandler(
            clientManager, new TestAdscContractRepository(), Substitute.For<IMediator>(), new TestClock(), Logger.None);

        var command = new RequestSelectedAdscContractCommand("YBBB", "UAL123", TimeSpan.FromMinutes(5));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var ybbbClient = (TestAcarsClient) await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        var ymmmClient = (TestAcarsClient) await clientManager.GetAcarsClient("hoppies-ymmm", CancellationToken.None);
        Assert.Equal(TimeSpan.FromMinutes(5), ybbbClient.RequestedAdscContracts["UAL123"]);
        Assert.Empty(ymmmClient.RequestedAdscContracts);
    }

    [Fact]
    public async Task Handle_WhenNoContractExists_AddsSelectedContract()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var mediator = Substitute.For<IMediator>();
        var handler = new RequestSelectedAdscContractCommandHandler(
            new TestClientManager(), contractRepository, mediator, clock, Logger.None);

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
        var clientManager = new TestClientManager();
        clientManager.AddConfiguration(TestClientManager.CreateConfiguration("YBBB", 10, selectedAdscContractDuration: 30));

        var contractRepository = new TestAdscContractRepository();
        var handler = new RequestSelectedAdscContractCommandHandler(
            clientManager, contractRepository, Substitute.For<IMediator>(), clock, Logger.None);

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
            new TestClientManager(), contractRepository, Substitute.For<IMediator>(), new TestClock(), Logger.None);

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
        var clientManager = new TestClientManager();
        var contractRepository = new TestAdscContractRepository();
        var handler = new RequestSelectedAdscContractCommandHandler(
            clientManager, contractRepository, Substitute.For<IMediator>(), new TestClock(), Logger.None);

        var command = new RequestSelectedAdscContractCommand("YBBB", "UAL123", AdscContract.MinimumReportingInterval - TimeSpan.FromSeconds(1));

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
        var handler = new RequestSelectedAdscContractCommandHandler(
            clientManager, new TestAdscContractRepository(), Substitute.For<IMediator>(), new TestClock(), Logger.None);

        var command = new RequestSelectedAdscContractCommand(
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
