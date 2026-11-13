using CPDLCServer.AdscContracts;
using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.AdscContracts;

public class RequestAdscContractOnConnectionEstablishedHandlerTests
{
    [Fact]
    public async Task Handle_RequestsDefaultContract()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var handler = new RequestAdscContractOnConnectionEstablishedHandler(
            new TestAcarsConnectionManager(), new TestAdscContractRepository(), mediator, Logger.None);

        var notification = new AircraftConnectionEstablished(
            "hoppies-ymmm", "YMMM", "UAL123", DataAuthorityState.NextDataAuthority);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<RequestPeriodicAdscContractCommand>(c => c.StationId == "YMMM" && c.Callsign == "UAL123" && c.ReportingInterval == TimeSpan.FromMinutes(20)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSelectedContractExists_DoesNotRequestContract()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10));
        contract.RequestSelectedContract(TimeSpan.FromMinutes(5), DateTimeOffset.UtcNow.AddHours(1));
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new RequestAdscContractOnConnectionEstablishedHandler(new TestAcarsConnectionManager(), contractRepository, mediator, Logger.None);

        var notification = new AircraftConnectionEstablished(
            "hoppies-ybbb", "YBBB", "UAL123", DataAuthorityState.NextDataAuthority);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_WhenOnlyDemandReportExists_RequestsDefaultContract()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10));
        contract.RequestDemandReport();
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new RequestAdscContractOnConnectionEstablishedHandler(new TestAcarsConnectionManager(), contractRepository, mediator, Logger.None);

        var notification = new AircraftConnectionEstablished(
            "hoppies-ybbb", "YBBB", "UAL123", DataAuthorityState.NextDataAuthority);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<RequestPeriodicAdscContractCommand>(c => c.StationId == "YBBB" && c.Callsign == "UAL123" && c.ReportingInterval == TimeSpan.FromMinutes(10)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDefaultContractIsRejected_RequestsDefaultContract()
    {
        // Arrange
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB", TimeSpan.FromMinutes(10));
        contract.RequestDefaultContract(TimeSpan.FromMinutes(10));
        contract.Reject();
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new RequestAdscContractOnConnectionEstablishedHandler(new TestAcarsConnectionManager(), contractRepository, mediator, Logger.None);

        var notification = new AircraftConnectionEstablished(
            "hoppies-ybbb", "YBBB", "UAL123", DataAuthorityState.NextDataAuthority);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<RequestPeriodicAdscContractCommand>(c => c.StationId == "YBBB" && c.Callsign == "UAL123" && c.ReportingInterval == TimeSpan.FromMinutes(10)),
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
            new TestAcarsConnectionManager(), new TestAdscContractRepository(), mediator, Logger.None);

        var notification = new AircraftConnectionEstablished(
            "hoppies-ybbb", "YBBB", "UAL123", DataAuthorityState.NextDataAuthority);

        // Act & Assert - should not throw
        await handler.Handle(notification, CancellationToken.None);
    }
}
