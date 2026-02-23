using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class CheckAircraftConnectionsRequestHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesLastSeenForConnectedAircraft()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var acarsClient = new TestAcarsClient();
        clientManager.AddClient("hoppies-ybbb", acarsClient);

        var aircraftRepository = new TestAircraftRepository();
        var acarsConnectedCallsignsRepository = new InMemoryAcarsConnectedCallsignsRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();

        var initialTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(initialTime);

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(initialTime);
        aircraft.AcceptLogon(initialTime);
        await aircraftRepository.Add(new AircraftKey(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        // Simulate the aircraft being connected on the ACARS network
        acarsClient.Connections.Add("UAL123");

        var handler = new CheckAircraftConnectionsRequestHandler(
            clientManager,
            aircraftRepository,
            acarsConnectedCallsignsRepository,
            mediator,
            clock,
            Logger.None);

        // Advance time
        var now = initialTime.AddMinutes(10);
        clock.SetUtcNow(now);

        var request = new CheckAircraftConnectionsRequest("hoppies-ybbb");

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        var updatedAircraft = await aircraftRepository.Find(new AircraftKey("UAL123", "hoppies-ybbb"), CancellationToken.None);
        Assert.NotNull(updatedAircraft);
        Assert.Equal(now, updatedAircraft.LastSeen);
    }

    [Fact]
    public async Task Handle_DoesNotUpdateLastSeenForDisconnectedAircraft()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var acarsClient = new TestAcarsClient();
        clientManager.AddClient("hoppies-ybbb", acarsClient);

        var aircraftRepository = new TestAircraftRepository();
        var acarsConnectedCallsignsRepository = new InMemoryAcarsConnectedCallsignsRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();

        var initialTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(initialTime);

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(initialTime);
        aircraft.AcceptLogon(initialTime);
        await aircraftRepository.Add(new AircraftKey(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        // Aircraft is NOT in the ACARS connections list (disconnected)
        // acarsClient.Connections remains empty

        var handler = new CheckAircraftConnectionsRequestHandler(
            clientManager,
            aircraftRepository,
            acarsConnectedCallsignsRepository,
            mediator,
            clock,
            Logger.None);

        // Advance time
        var now = initialTime.AddMinutes(10);
        clock.SetUtcNow(now);

        var request = new CheckAircraftConnectionsRequest("hoppies-ybbb");

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert - LastSeen should still be the initial time (from AcceptLogon)
        var unchangedAircraft = await aircraftRepository.Find(new AircraftKey("UAL123", "hoppies-ybbb"), CancellationToken.None);
        Assert.NotNull(unchangedAircraft);
        Assert.Equal(initialTime, unchangedAircraft.LastSeen);
    }

    [Fact]
    public async Task Handle_OnlyUpdatesAircraftForMatchingAcarsClient()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var acarsClientYbbb = new TestAcarsClient();
        var acarsClientYmmm = new TestAcarsClient();
        clientManager.AddClient("hoppies-ybbb", acarsClientYbbb);
        clientManager.AddClient("hoppies-ymmm", acarsClientYmmm);

        var aircraftRepository = new TestAircraftRepository();
        var acarsConnectedCallsignsRepository = new InMemoryAcarsConnectedCallsignsRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();

        var initialTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(initialTime);

        // Aircraft on YBBB
        var aircraftYbbb = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraftYbbb.RequestLogon(initialTime);
        aircraftYbbb.AcceptLogon(initialTime);
        await aircraftRepository.Add(new AircraftKey(aircraftYbbb.Callsign, aircraftYbbb.AcarsClientId), aircraftYbbb, CancellationToken.None);

        // Aircraft on YMMM
        var aircraftYmmm = new AircraftConnection("QFA456", "hoppies-ymmm", "YMMM", DataAuthorityState.CurrentDataAuthority);
        aircraftYmmm.RequestLogon(initialTime);
        aircraftYmmm.AcceptLogon(initialTime);
        await aircraftRepository.Add(new AircraftKey(aircraftYmmm.Callsign, aircraftYmmm.AcarsClientId), aircraftYmmm, CancellationToken.None);

        // Both aircraft are connected on their respective ACARS networks
        acarsClientYbbb.Connections.Add("UAL123");
        acarsClientYmmm.Connections.Add("QFA456");

        var handler = new CheckAircraftConnectionsRequestHandler(
            clientManager,
            aircraftRepository,
            acarsConnectedCallsignsRepository,
            mediator,
            clock,
            Logger.None);

        // Advance time
        var now = initialTime.AddMinutes(10);
        clock.SetUtcNow(now);

        // Only check YBBB
        var request = new CheckAircraftConnectionsRequest("hoppies-ybbb");

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert - only YBBB aircraft should be updated
        var updatedYbbb = await aircraftRepository.Find(new AircraftKey("UAL123", "hoppies-ybbb"), CancellationToken.None);
        Assert.NotNull(updatedYbbb);
        Assert.Equal(now, updatedYbbb.LastSeen);

        var unchangedYmmm = await aircraftRepository.Find(new AircraftKey("QFA456", "hoppies-ymmm"), CancellationToken.None);
        Assert.NotNull(unchangedYmmm);
        Assert.Equal(initialTime, unchangedYmmm.LastSeen);
    }

    [Fact]
    public async Task Handle_UpdatesRepositoryWithConnectedCallsigns()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var acarsClient = new TestAcarsClient();
        clientManager.AddClient("hoppies-ybbb", acarsClient);

        var aircraftRepository = new TestAircraftRepository();
        var acarsConnectedCallsignsRepository = new InMemoryAcarsConnectedCallsignsRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();

        var initialTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(initialTime);

        // Simulate aircraft connected on the ACARS network
        acarsClient.Connections.Add("UAL123");
        acarsClient.Connections.Add("QFA456");

        var handler = new CheckAircraftConnectionsRequestHandler(
            clientManager,
            aircraftRepository,
            acarsConnectedCallsignsRepository,
            mediator,
            clock,
            Logger.None);

        var request = new CheckAircraftConnectionsRequest("hoppies-ybbb");

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        var callsigns = await acarsConnectedCallsignsRepository.All(CancellationToken.None);
        Assert.Equal(2, callsigns.Length);
        Assert.Contains("UAL123", callsigns);
        Assert.Contains("QFA456", callsigns);
    }

    [Fact]
    public async Task Handle_PublishesNotificationWhenCallsignsChange()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var acarsClient = new TestAcarsClient();
        clientManager.AddClient("hoppies-ybbb", acarsClient);

        var aircraftRepository = new TestAircraftRepository();
        var acarsConnectedCallsignsRepository = new InMemoryAcarsConnectedCallsignsRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();

        var initialTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(initialTime);

        // Simulate aircraft connected on the ACARS network
        acarsClient.Connections.Add("UAL123");

        var handler = new CheckAircraftConnectionsRequestHandler(
            clientManager,
            aircraftRepository,
            acarsConnectedCallsignsRepository,
            mediator,
            clock,
            Logger.None);

        var request = new CheckAircraftConnectionsRequest("hoppies-ybbb");

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(
            Arg.Is<AcarsConnectedCallsignsChangedNotification>(n =>
                n.Callsigns.Length == 1 && n.Callsigns.Contains("UAL123")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotPublishNotificationWhenCallsignsUnchanged()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var acarsClient = new TestAcarsClient();
        clientManager.AddClient("hoppies-ybbb", acarsClient);

        var aircraftRepository = new TestAircraftRepository();
        var acarsConnectedCallsignsRepository = new InMemoryAcarsConnectedCallsignsRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();

        var initialTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(initialTime);

        // Simulate aircraft connected on the ACARS network
        acarsClient.Connections.Add("UAL123");

        // Pre-populate the repository with the same callsign
        await acarsConnectedCallsignsRepository.Update(["UAL123"], CancellationToken.None);

        var handler = new CheckAircraftConnectionsRequestHandler(
            clientManager,
            aircraftRepository,
            acarsConnectedCallsignsRepository,
            mediator,
            clock,
            Logger.None);

        var request = new CheckAircraftConnectionsRequest("hoppies-ybbb");

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Publish(
            Arg.Any<AcarsConnectedCallsignsChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PublishesNotificationWhenCallsignAdded()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var acarsClient = new TestAcarsClient();
        clientManager.AddClient("hoppies-ybbb", acarsClient);

        var aircraftRepository = new TestAircraftRepository();
        var acarsConnectedCallsignsRepository = new InMemoryAcarsConnectedCallsignsRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();

        var initialTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(initialTime);

        // Pre-populate the repository with one callsign
        await acarsConnectedCallsignsRepository.Update(["UAL123"], CancellationToken.None);

        // Simulate a new aircraft joining the ACARS network
        acarsClient.Connections.Add("UAL123");
        acarsClient.Connections.Add("QFA456");

        var handler = new CheckAircraftConnectionsRequestHandler(
            clientManager,
            aircraftRepository,
            acarsConnectedCallsignsRepository,
            mediator,
            clock,
            Logger.None);

        var request = new CheckAircraftConnectionsRequest("hoppies-ybbb");

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(
            Arg.Is<AcarsConnectedCallsignsChangedNotification>(n =>
                n.Callsigns.Length == 2 &&
                n.Callsigns.Contains("UAL123") &&
                n.Callsigns.Contains("QFA456")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PublishesNotificationWhenCallsignRemoved()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var acarsClient = new TestAcarsClient();
        clientManager.AddClient("hoppies-ybbb", acarsClient);

        var aircraftRepository = new TestAircraftRepository();
        var acarsConnectedCallsignsRepository = new InMemoryAcarsConnectedCallsignsRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();

        var initialTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(initialTime);

        // Pre-populate the repository with two callsigns
        await acarsConnectedCallsignsRepository.Update(["UAL123", "QFA456"], CancellationToken.None);

        // Simulate one aircraft leaving the ACARS network
        acarsClient.Connections.Add("UAL123");
        // QFA456 is no longer connected

        var handler = new CheckAircraftConnectionsRequestHandler(
            clientManager,
            aircraftRepository,
            acarsConnectedCallsignsRepository,
            mediator,
            clock,
            Logger.None);

        var request = new CheckAircraftConnectionsRequest("hoppies-ybbb");

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(
            Arg.Is<AcarsConnectedCallsignsChangedNotification>(n =>
                n.Callsigns.Length == 1 && n.Callsigns.Contains("UAL123")),
            Arg.Any<CancellationToken>());
    }
}
