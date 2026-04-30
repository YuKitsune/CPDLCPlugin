using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class LogonCommandHandlerTests
{
    [Fact]
    public async Task Handle_NoATC_RejectsLogon()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var mediator = Substitute.For<IMediator>();

        var command = new LogonCommand(1, "QFA1", "hoppies-ybbb");

        var handler = new LogonCommandHandler(
            new TestClientManager(),
            aircraftRepository,
            controllerRepository,
            clock,
            mediator,
            Logger.None);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(new SendUplinkCommand(
            "SYSTEM",
            "QFA1",
            1,
            CpdlcUplinkResponseType.NoResponse,
            "LOGON REJECTED. NO ATS AVBL."),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewConnection_AcceptsLogon()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var mediator = Substitute.For<IMediator>();

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "Connection-1",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");

        await controllerRepository.Add(controller, CancellationToken.None);

        var command = new LogonCommand(1, "QFA1", "hoppies-ybbb");

        var handler = new LogonCommandHandler(
            new TestClientManager(),
            aircraftRepository,
            controllerRepository,
            clock,
            mediator,
            Logger.None);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(new SendUplinkCommand(
                "SYSTEM",
                "QFA1",
                1,
                CpdlcUplinkResponseType.NoResponse,
                "LOGON ACCEPTED"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewConnection_TracksConnection()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var mediator = Substitute.For<IMediator>();

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "Connection-1",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");

        await controllerRepository.Add(controller, CancellationToken.None);

        var command = new LogonCommand(1, "QFA1", "hoppies-ybbb");

        var handler = new LogonCommandHandler(
            new TestClientManager(),
            aircraftRepository,
            controllerRepository,
            clock,
            mediator,
            Logger.None);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var trackedAircraft = await aircraftRepository.Find(new("QFA1", "hoppies-ybbb"), CancellationToken.None);
        Assert.NotNull(trackedAircraft);
        Assert.Equal(DataAuthorityState.NextDataAuthority, trackedAircraft.DataAuthorityState);
        Assert.Equal(clock.UtcNow(), trackedAircraft.LogonRequested);
        Assert.Equal(clock.UtcNow(), trackedAircraft.LogonAccepted);
        Assert.Equal(clock.UtcNow(), trackedAircraft.LastSeen);
    }

    [Fact]
    public async Task Handle_ExistingConnection_AcceptsLogon_AndDoesNotDuplicateConnection()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var mediator = Substitute.For<IMediator>();

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "Connection-1",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");

        await controllerRepository.Add(controller, CancellationToken.None);

        var command = new LogonCommand(1, "QFA1", "hoppies-ybbb");

        var handler = new LogonCommandHandler(
            new TestClientManager(),
            aircraftRepository,
            controllerRepository,
            clock,
            mediator,
            Logger.None);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(new SendUplinkCommand(
                "SYSTEM",
                "QFA1",
                1,
                CpdlcUplinkResponseType.NoResponse,
                "LOGON ACCEPTED"),
            Arg.Any<CancellationToken>());

        var allTrackedAircraft = await aircraftRepository.All(CancellationToken.None);
        Assert.Single(allTrackedAircraft);
        Assert.Equal("QFA1", allTrackedAircraft.Single().Callsign);
    }

    [Fact]
    public async Task Handle_NotifiesATC()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var mediator = Substitute.For<IMediator>();

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "Connection-1",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");

        await controllerRepository.Add(controller, CancellationToken.None);

        var command = new LogonCommand(1, "QFA1", "hoppies-ybbb");

        var handler = new LogonCommandHandler(
            new TestClientManager(),
            aircraftRepository,
            controllerRepository,
            clock,
            mediator,
            Logger.None);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(new AircraftConnectionEstablished(
                "hoppies-ybbb",
                "YBBB",
                "QFA1",
                DataAuthorityState.NextDataAuthority),
            Arg.Any<CancellationToken>());
    }
}
