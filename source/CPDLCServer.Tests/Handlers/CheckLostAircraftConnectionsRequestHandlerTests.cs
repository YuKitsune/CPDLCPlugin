using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class CheckLostAircraftConnectionsRequestHandlerTests
{
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
        await aircraftRepository.Add(new AircraftKey(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new CheckLostAircraftConnectionsRequestHandler(
            aircraftRepository,
            clock,
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
            Arg.Is<AircraftConnectionLost>(n => n.AcarsClientId == "hoppies-ybbb" && n.Callsign == "UAL123"),
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
        await aircraftRepository.Add(new AircraftKey(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new CheckLostAircraftConnectionsRequestHandler(
            aircraftRepository,
            clock,
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
        await aircraftRepository.Add(new AircraftKey(aircraft1.Callsign, aircraft1.AcarsClientId), aircraft1, CancellationToken.None);

        var aircraft2 = new AircraftConnection("QFA456", "hoppies-ymmm", "YMMM", DataAuthorityState.CurrentDataAuthority);
        aircraft2.RequestLogon(initialTime);
        aircraft2.AcceptLogon(initialTime);
        await aircraftRepository.Add(new AircraftKey(aircraft2.Callsign, aircraft2.AcarsClientId), aircraft2, CancellationToken.None);

        var handler = new CheckLostAircraftConnectionsRequestHandler(
            aircraftRepository,
            clock,
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
            Arg.Is<AircraftConnectionLost>(n => n.AcarsClientId == "hoppies-ybbb" && n.Callsign == "UAL123"),
            Arg.Any<CancellationToken>());

        await mediator.Received(1).Publish(
            Arg.Is<AircraftConnectionLost>(n => n.AcarsClientId == "hoppies-ymmm" && n.Callsign == "QFA456"),
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
        await aircraftRepository.Add(new AircraftKey(oldAircraft.Callsign, oldAircraft.AcarsClientId), oldAircraft, CancellationToken.None);

        // Advance time past the timeout
        var timeout = TimeSpan.FromMinutes(15);
        var now = initialTime.Add(timeout).AddMinutes(1);
        clock.SetUtcNow(now);

        // New aircraft that was recently seen
        var newAircraft = new AircraftConnection("QFA456", "hoppies-ymmm", "YMMM", DataAuthorityState.CurrentDataAuthority);
        newAircraft.RequestLogon(now);
        newAircraft.AcceptLogon(now);
        await aircraftRepository.Add(new AircraftKey(newAircraft.Callsign, newAircraft.AcarsClientId), newAircraft, CancellationToken.None);

        var handler = new CheckLostAircraftConnectionsRequestHandler(
            aircraftRepository,
            clock,
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
