using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class CancelAdscContractOnConnectionEndedHandlerTests
{
    [Fact]
    public async Task Handle_ConnectionTerminated_CancelsContract()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB");
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new CancelAdscContractOnConnectionEndedHandler(
            new TestClientManager(), contractRepository, mediator, Logger.None);

        // Act
        await handler.Handle(
            new AircraftConnectionTerminated("hoppies-ybbb", "YBBB", "UAL123"),
            CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<CancelAdscContractCommand>(c => c.StationId == "YBBB" && c.Callsign == "UAL123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConnectionLost_CancelsContractForStationOfAcarsClient()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YMMM");
        await contractRepository.Add(new AdscContractKey("UAL123", "YMMM"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new CancelAdscContractOnConnectionEndedHandler(
            new TestClientManager(), contractRepository, mediator, Logger.None);

        // Act
        await handler.Handle(
            new AircraftConnectionLost("hoppies-ymmm", "UAL123"),
            CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<CancelAdscContractCommand>(c => c.StationId == "YMMM" && c.Callsign == "UAL123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoContract_DoesNotCancel()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var handler = new CancelAdscContractOnConnectionEndedHandler(
            new TestClientManager(), new TestAdscContractRepository(), mediator, Logger.None);

        // Act
        await handler.Handle(
            new AircraftConnectionTerminated("hoppies-ybbb", "YBBB", "UAL123"),
            CancellationToken.None);

        // Assert
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_WhenContractIsWithOtherStation_DoesNotCancel()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YMMM");
        await contractRepository.Add(new AdscContractKey("UAL123", "YMMM"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new CancelAdscContractOnConnectionEndedHandler(
            new TestClientManager(), contractRepository, mediator, Logger.None);

        // Act
        await handler.Handle(
            new AircraftConnectionTerminated("hoppies-ybbb", "YBBB", "UAL123"),
            CancellationToken.None);

        // Assert
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_WhenCancelFails_DoesNotThrow()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB");
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        mediator
            .Send(Arg.Any<CancelAdscContractCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("Hoppie is down")));

        var handler = new CancelAdscContractOnConnectionEndedHandler(
            new TestClientManager(), contractRepository, mediator, Logger.None);

        // Act & Assert - should not throw
        await handler.Handle(
            new AircraftConnectionTerminated("hoppies-ybbb", "YBBB", "UAL123"),
            CancellationToken.None);
    }
}
