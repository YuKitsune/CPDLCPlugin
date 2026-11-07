using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Infrastructure;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Cpdlc.Connections;

public class CheckLostAircraftConnectionsRequestHandlerTests
{
    // The server has been online for longer than any timeout
    static readonly ServerStartTime StartedLongAgo = new(DateTimeOffset.MinValue);

    [Fact]
    public async Task Handle_WhenServerOnlineForLessThanTimeout_DoesNotPublishAircraftLost()
    {
        // Arrange - the aircraft was last seen long before the server started
        var aircraftRepository = new TestAircraftRepository();
        var clock = new TestClock();
        var mediator = Substitute.For<IMediator>();

        var lastSeen = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(lastSeen);

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(lastSeen);
        aircraft.AcceptLogon(lastSeen);
        await aircraftRepository.Add(new AircraftKey(aircraft.Callsign, aircraft.AcarsConnectionId), aircraft, CancellationToken.None);

        var timeout = TimeSpan.FromMinutes(15);
        var serverStarted = lastSeen.AddHours(2);
        var handler = new CheckLostAircraftConnectionsRequestHandler(
            aircraftRepository,
            clock,
            new ServerStartTime(serverStarted),
            mediator,
            Logger.None);

        clock.SetUtcNow(serverStarted + timeout);

        // Act
        await handler.Handle(new CheckLostAircraftConnectionsRequest(timeout), CancellationToken.None);

        // Assert
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_WhenServerOnlineForLongerThanTimeout_PublishesAircraftLost()
    {
        // Arrange - the aircraft was last seen long before the server started
        var aircraftRepository = new TestAircraftRepository();
        var clock = new TestClock();
        var mediator = Substitute.For<IMediator>();

        var lastSeen = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(lastSeen);

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(lastSeen);
        aircraft.AcceptLogon(lastSeen);
        await aircraftRepository.Add(new AircraftKey(aircraft.Callsign, aircraft.AcarsConnectionId), aircraft, CancellationToken.None);

        var timeout = TimeSpan.FromMinutes(15);
        var serverStarted = lastSeen.AddHours(2);
        var handler = new CheckLostAircraftConnectionsRequestHandler(
            aircraftRepository,
            clock,
            new ServerStartTime(serverStarted),
            mediator,
            Logger.None);

        clock.SetUtcNow(serverStarted + timeout + TimeSpan.FromMinutes(1));

        // Act
        await handler.Handle(new CheckLostAircraftConnectionsRequest(timeout), CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(
            Arg.Is<AircraftConnectionLost>(n => n.AcarsConnectionId == "hoppies-ybbb" && n.Callsign == "UAL123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PublishesAircraftLostWhenConnectionTimedOut()
    {
        // Arrange
        var aircraftRepository = new TestAircraftRepository();
        var clock = new TestClock();
        var mediator = Substitute.For<IMediator>();

        var initialTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(initialTime);

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(initialTime);
        aircraft.AcceptLogon(initialTime);
        await aircraftRepository.Add(new AircraftKey(aircraft.Callsign, aircraft.AcarsConnectionId), aircraft, CancellationToken.None);

        var handler = new CheckLostAircraftConnectionsRequestHandler(
            aircraftRepository,
            clock,
            StartedLongAgo,
            mediator,
            Logger.None);

        // Advance time past the timeout
        var timeout = TimeSpan.FromMinutes(15);
        var now = initialTime.Add(timeout).AddMinutes(1);
        clock.SetUtcNow(now);

        var request = new CheckLostAircraftConnectionsRequest(timeout);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(
            Arg.Is<AircraftConnectionLost>(n => n.AcarsConnectionId == "hoppies-ybbb" && n.Callsign == "UAL123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotPublishAircraftLostWhenConnectionNotTimedOut()
    {
        // Arrange
        var aircraftRepository = new TestAircraftRepository();
        var clock = new TestClock();
        var mediator = Substitute.For<IMediator>();

        var initialTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(initialTime);

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(initialTime);
        aircraft.AcceptLogon(initialTime);
        await aircraftRepository.Add(new AircraftKey(aircraft.Callsign, aircraft.AcarsConnectionId), aircraft, CancellationToken.None);

        var handler = new CheckLostAircraftConnectionsRequestHandler(
            aircraftRepository,
            clock,
            StartedLongAgo,
            mediator,
            Logger.None);

        // Advance time but stay within the timeout window
        var timeout = TimeSpan.FromMinutes(15);
        var now = initialTime.AddMinutes(15);
        clock.SetUtcNow(now);

        var request = new CheckLostAircraftConnectionsRequest(timeout);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Publish(
            Arg.Any<AircraftConnectionLost>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PublishesAircraftLostForEachTimedOutAircraft()
    {
        // Arrange
        var aircraftRepository = new TestAircraftRepository();
        var clock = new TestClock();
        var mediator = Substitute.For<IMediator>();

        var initialTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(initialTime);

        var aircraft1 = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft1.RequestLogon(initialTime);
        aircraft1.AcceptLogon(initialTime);
        await aircraftRepository.Add(new AircraftKey(aircraft1.Callsign, aircraft1.AcarsConnectionId), aircraft1, CancellationToken.None);

        var aircraft2 = new AircraftConnection("QFA456", "hoppies-ymmm", "YMMM", DataAuthorityState.CurrentDataAuthority);
        aircraft2.RequestLogon(initialTime);
        aircraft2.AcceptLogon(initialTime);
        await aircraftRepository.Add(new AircraftKey(aircraft2.Callsign, aircraft2.AcarsConnectionId), aircraft2, CancellationToken.None);

        var handler = new CheckLostAircraftConnectionsRequestHandler(
            aircraftRepository,
            clock,
            StartedLongAgo,
            mediator,
            Logger.None);

        // Advance time past the timeout
        var timeout = TimeSpan.FromMinutes(15);
        var now = initialTime.Add(timeout).AddMinutes(1);
        clock.SetUtcNow(now);

        var request = new CheckLostAircraftConnectionsRequest(timeout);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(
            Arg.Is<AircraftConnectionLost>(n => n.AcarsConnectionId == "hoppies-ybbb" && n.Callsign == "UAL123"),
            Arg.Any<CancellationToken>());

        await mediator.Received(1).Publish(
            Arg.Is<AircraftConnectionLost>(n => n.AcarsConnectionId == "hoppies-ymmm" && n.Callsign == "QFA456"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnlyPublishesForTimedOutAircraft()
    {
        // Arrange
        var aircraftRepository = new TestAircraftRepository();
        var clock = new TestClock();
        var mediator = Substitute.For<IMediator>();

        var initialTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(initialTime);

        // Old aircraft that will time out
        var oldAircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        oldAircraft.RequestLogon(initialTime);
        oldAircraft.AcceptLogon(initialTime);
        await aircraftRepository.Add(new AircraftKey(oldAircraft.Callsign, oldAircraft.AcarsConnectionId), oldAircraft, CancellationToken.None);

        // Advance time past the timeout
        var timeout = TimeSpan.FromMinutes(15);
        var now = initialTime.Add(timeout).AddMinutes(1);
        clock.SetUtcNow(now);

        // New aircraft that was recently seen
        var newAircraft = new AircraftConnection("QFA456", "hoppies-ymmm", "YMMM", DataAuthorityState.CurrentDataAuthority);
        newAircraft.RequestLogon(now);
        newAircraft.AcceptLogon(now);
        await aircraftRepository.Add(new AircraftKey(newAircraft.Callsign, newAircraft.AcarsConnectionId), newAircraft, CancellationToken.None);

        var handler = new CheckLostAircraftConnectionsRequestHandler(
            aircraftRepository,
            clock,
            StartedLongAgo,
            mediator,
            Logger.None);

        var request = new CheckLostAircraftConnectionsRequest(timeout);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert - only old aircraft should be published
        await mediator.Received(1).Publish(
            Arg.Is<AircraftConnectionLost>(n => n.Callsign == "UAL123"),
            Arg.Any<CancellationToken>());

        await mediator.DidNotReceive().Publish(
            Arg.Is<AircraftConnectionLost>(n => n.Callsign == "QFA456"),
            Arg.Any<CancellationToken>());
    }
}
