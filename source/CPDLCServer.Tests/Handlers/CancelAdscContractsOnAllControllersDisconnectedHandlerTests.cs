using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class CancelAdscContractsOnAllControllersDisconnectedHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoControllersRemain_CancelsAllContracts()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        await contractRepository.Add(
            new AdscContractKey("UAL123", "YBBB"),
            new AdscContract("UAL123", "YBBB"),
            CancellationToken.None);
        await contractRepository.Add(
            new AdscContractKey("QFA1", "YMMM"),
            new AdscContract("QFA1", "YMMM"),
            CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new CancelAdscContractsOnAllControllersDisconnectedHandler(
            new TestControllerRepository(), contractRepository, mediator, Logger.None);

        // Act
        await handler.Handle(
            new ControllerDisconnectedNotification(Guid.NewGuid(), "BN-TSN_CTR"),
            CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<CancelAdscContractCommand>(c => c.StationId == "YBBB" && c.Callsign == "UAL123"),
            Arg.Any<CancellationToken>());
        await mediator.Received(1).Send(
            Arg.Is<CancelAdscContractCommand>(c => c.StationId == "YMMM" && c.Callsign == "QFA1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenControllersRemain_DoesNotCancel()
    {
        // Arrange
        var controllerRepository = new TestControllerRepository();
        await controllerRepository.Add(
            new ControllerInfo(Guid.NewGuid(), "connection-1", "YBBB", "ML-ASP_CTR", "1234567"),
            CancellationToken.None);

        var contractRepository = new TestAdscContractRepository();
        await contractRepository.Add(
            new AdscContractKey("UAL123", "YBBB"),
            new AdscContract("UAL123", "YBBB"),
            CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new CancelAdscContractsOnAllControllersDisconnectedHandler(
            controllerRepository, contractRepository, mediator, Logger.None);

        // Act
        await handler.Handle(
            new ControllerDisconnectedNotification(Guid.NewGuid(), "BN-TSN_CTR"),
            CancellationToken.None);

        // Assert
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_WhenCancelFails_CancelsOtherContracts()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        await contractRepository.Add(
            new AdscContractKey("UAL123", "YBBB"),
            new AdscContract("UAL123", "YBBB"),
            CancellationToken.None);
        await contractRepository.Add(
            new AdscContractKey("QFA1", "YMMM"),
            new AdscContract("QFA1", "YMMM"),
            CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        mediator
            .Send(Arg.Is<CancelAdscContractCommand>(c => c.Callsign == "UAL123"), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("Hoppie is down")));

        var handler = new CancelAdscContractsOnAllControllersDisconnectedHandler(
            new TestControllerRepository(), contractRepository, mediator, Logger.None);

        // Act
        await handler.Handle(
            new ControllerDisconnectedNotification(Guid.NewGuid(), "BN-TSN_CTR"),
            CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<CancelAdscContractCommand>(c => c.StationId == "YMMM" && c.Callsign == "QFA1"),
            Arg.Any<CancellationToken>());
    }
}
