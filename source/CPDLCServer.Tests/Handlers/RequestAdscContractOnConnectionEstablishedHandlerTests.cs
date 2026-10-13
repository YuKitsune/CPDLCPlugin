using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class RequestAdscContractOnConnectionEstablishedHandlerTests
{
    [Fact]
    public async Task Handle_RequestsContractWithDefaultIntervalForStation()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var handler = new RequestAdscContractOnConnectionEstablishedHandler(
            new TestClientManager(), new TestAdscContractRepository(), mediator, Logger.None);

        var notification = new AircraftConnectionEstablished(
            "hoppies-ymmm", "YMMM", "UAL123", DataAuthorityState.NextDataAuthority);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - TestClientManager sets the YMMM default to 20 minutes
        await mediator.Received(1).Send(
            Arg.Is<RequestPeriodicAdscContractCommand>(c =>
                c.StationId == "YMMM" &&
                c.Callsign == "UAL123" &&
                c.ReportingInterval == TimeSpan.FromMinutes(20)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPeriodicContractExists_DoesNotRequestContract()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(5), DateTimeOffset.UtcNow);
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new RequestAdscContractOnConnectionEstablishedHandler(
            new TestClientManager(), contractRepository, mediator, Logger.None);

        var notification = new AircraftConnectionEstablished(
            "hoppies-ybbb", "YBBB", "UAL123", DataAuthorityState.NextDataAuthority);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_WhenOnlyDemandContractExists_RequestsContract()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", null, DateTimeOffset.UtcNow);
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new RequestAdscContractOnConnectionEstablishedHandler(
            new TestClientManager(), contractRepository, mediator, Logger.None);

        var notification = new AircraftConnectionEstablished(
            "hoppies-ybbb", "YBBB", "UAL123", DataAuthorityState.NextDataAuthority);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - TestClientManager sets the YBBB default to 10 minutes
        await mediator.Received(1).Send(
            Arg.Is<RequestPeriodicAdscContractCommand>(c =>
                c.StationId == "YBBB" &&
                c.Callsign == "UAL123" &&
                c.ReportingInterval == TimeSpan.FromMinutes(10)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRequestFails_DoesNotThrow()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        mediator
            .Send(Arg.Any<RequestPeriodicAdscContractCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("Hoppie is down")));

        var handler = new RequestAdscContractOnConnectionEstablishedHandler(
            new TestClientManager(), new TestAdscContractRepository(), mediator, Logger.None);

        var notification = new AircraftConnectionEstablished(
            "hoppies-ybbb", "YBBB", "UAL123", DataAuthorityState.NextDataAuthority);

        // Act & Assert - should not throw
        await handler.Handle(notification, CancellationToken.None);
    }
}
