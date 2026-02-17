using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;

namespace CPDLCServer.Tests.Handlers;

public class GetConnectedAircraftRequestHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllConnectedAircraft()
    {
        // Arrange
        var aircraftManager = new TestAircraftRepository();
        var handler = new GetConnectedAircraftRequestHandler(aircraftManager);

        // Add test aircraft on different networks/stations
        var aircraft1 = new AircraftConnection(
            "UAL123",
            "hoppies-ybbb",
            "YBBB",
            DataAuthorityState.CurrentDataAuthority);
        aircraft1.RequestLogon(DateTimeOffset.UtcNow);
        aircraft1.AcceptLogon(DateTimeOffset.UtcNow);
        await aircraftManager.Add(new(aircraft1.Callsign, aircraft1.AcarsClientId), aircraft1, CancellationToken.None);

        var aircraft2 = new AircraftConnection(
            "QFA456",
            "hoppies-ybbb",
            "YBBB",
            DataAuthorityState.CurrentDataAuthority);
        aircraft2.RequestLogon(DateTimeOffset.UtcNow);
        await aircraftManager.Add(new(aircraft2.Callsign, aircraft2.AcarsClientId), aircraft2, CancellationToken.None);

        // Aircraft on different ACARS client - should also be returned
        var aircraft3 = new AircraftConnection(
            "AAL789",
            "hoppies-ymmm",
            "YMMM",
            DataAuthorityState.CurrentDataAuthority);
        aircraft3.RequestLogon(DateTimeOffset.UtcNow);
        await aircraftManager.Add(new(aircraft3.Callsign, aircraft3.AcarsClientId), aircraft3, CancellationToken.None);

        var query = new GetConnectedAircraftRequest();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - returns ALL aircraft regardless of ACARS client
        Assert.NotNull(result);
        Assert.Equal(3, result.Aircraft.Length);
        Assert.Contains(result.Aircraft, a => a.Callsign == "UAL123");
        Assert.Contains(result.Aircraft, a => a.Callsign == "QFA456");
        Assert.Contains(result.Aircraft, a => a.Callsign == "AAL789");
    }

    [Fact]
    public async Task Handle_ReturnsEmptyArrayWhenNoAircraft()
    {
        // Arrange
        var aircraftManager = new TestAircraftRepository();
        var handler = new GetConnectedAircraftRequestHandler(aircraftManager);

        var query = new GetConnectedAircraftRequest();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Aircraft);
    }

    [Fact]
    public async Task Handle_IncludesAllConnectionState()
    {
        // Arrange
        var aircraftManager = new TestAircraftRepository();
        var handler = new GetConnectedAircraftRequestHandler(aircraftManager);

        var aircraft = new AircraftConnection(
            "UAL123",
            "hoppies-ybbb",
            "YBBB",
            DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(DateTimeOffset.UtcNow);
        aircraft.AcceptLogon(DateTimeOffset.UtcNow);
        await aircraftManager.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var query = new GetConnectedAircraftRequest();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var aircraftInfo = Assert.Single(result.Aircraft);
        Assert.Equal("UAL123", aircraftInfo.Callsign);
        Assert.Equal("YBBB", aircraftInfo.StationId);
        Assert.Equal(Contracts.DataAuthorityState.CurrentDataAuthority, aircraftInfo.DataAuthorityState);
    }
}
