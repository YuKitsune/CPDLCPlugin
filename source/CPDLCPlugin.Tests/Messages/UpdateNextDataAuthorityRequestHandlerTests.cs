using CPDLCPlugin.Configuration;
using CPDLCPlugin.Messages;
using CPDLCPlugin.Server;
using CPDLCServer.Contracts;
using NSubstitute;
using Serilog.Core;

namespace CPDLCPlugin.Tests.Messages;

public class UpdateNextDataAuthorityRequestHandlerTests
{
    const string Callsign = "QFA123";

    const string BrisbaneSector = "BN-ARL";
    const string MelbourneSector = "ML-ASP";
    const string AucklandSector = "NZ-OCE";
    const string AustraliaSectorGroup = "AUSTRALIA";

    static readonly DateTimeOffset Now = new(2026, 10, 17, 10, 0, 0, TimeSpan.Zero);

    readonly IServerConnection _serverConnection = Substitute.For<IServerConnection>();
    readonly IErrorReporter _errorReporter = Substitute.For<IErrorReporter>();
    readonly AircraftConnectionStore _aircraftConnectionStore = new();
    readonly AcarsStationStore _acarsStationStore = new();

    public UpdateNextDataAuthorityRequestHandlerTests()
    {
        _serverConnection.IsConnected.Returns(true);
        _serverConnection.StationIdentifier.Returns("YBBB");
    }

    [Fact]
    public async Task Handle_WhenNextSectorIsOnline_SendsNextDataAuthority()
    {
        // Arrange
        var aircraft = await AddAircraft("YBBB", DataAuthorityState.CurrentDataAuthority);
        await _acarsStationStore.Populate(["YBBB", "YMMM"]);

        var handler = CreateHandler();
        var request = new UpdateNextDataAuthorityRequest(
            Callsign,
            [
                new SectorEntry([BrisbaneSector], Now.AddMinutes(10)),
                new SectorEntry([MelbourneSector], Now.AddMinutes(30))
            ]);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        await _serverConnection.Received(1).UpdateNextDataAuthority(
            Callsign,
            "YMMM",
            Now.AddMinutes(30),
            Arg.Any<CancellationToken>());

        var info = Assert.IsType<ValidNextDataAuthorityInfo>(aircraft.NextDataAuthorityInfo);
        Assert.Equal("YMMM", info.NextDataAuthority);
        Assert.Equal(Now.AddMinutes(30), info.ExitTime);
    }

    [Fact]
    public async Task Handle_WhenAircraftWasHandedOffEarly_DoesNotSendPreviousSectorAsNextDataAuthority()
    {
        // Arrange - the aircraft is still in NZZO airspace, but YBBB is already the CDA
        var aircraft = await AddAircraft("YBBB", DataAuthorityState.CurrentDataAuthority);
        await _acarsStationStore.Populate(["YBBB", "NZZO"]);

        var handler = CreateHandler();
        var request = new UpdateNextDataAuthorityRequest(
            Callsign,
            [
                new SectorEntry([AucklandSector], Now.AddMinutes(5)),
                new SectorEntry([BrisbaneSector], Now.AddMinutes(15))
            ]);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        await _serverConnection.DidNotReceiveWithAnyArgs().UpdateNextDataAuthority(
            default!,
            default,
            default,
            default);

        Assert.IsType<NoneNextDataAuthorityInfo>(aircraft.NextDataAuthorityInfo);
    }

    [Fact]
    public async Task Handle_WhenAircraftWasHandedOffEarlyAndLeavesAgain_SendsNextSectorAfterOurAirspace()
    {
        // Arrange
        var aircraft = await AddAircraft("YBBB", DataAuthorityState.CurrentDataAuthority);
        await _acarsStationStore.Populate(["YBBB", "NZZO", "YMMM"]);

        var handler = CreateHandler();
        var request = new UpdateNextDataAuthorityRequest(
            Callsign,
            [
                new SectorEntry([AucklandSector], Now.AddMinutes(5)),
                new SectorEntry([BrisbaneSector], Now.AddMinutes(15)),
                new SectorEntry([MelbourneSector], Now.AddMinutes(45))
            ]);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        await _serverConnection.Received(1).UpdateNextDataAuthority(
            Callsign,
            "YMMM",
            Now.AddMinutes(45),
            Arg.Any<CancellationToken>());

        var info = Assert.IsType<ValidNextDataAuthorityInfo>(aircraft.NextDataAuthorityInfo);
        Assert.Equal("YMMM", info.NextDataAuthority);
    }

    [Fact]
    public async Task Handle_WhenMostSpecificAtsuIsOffline_FallsBackToSectorGroupAtsu()
    {
        // Arrange
        var aircraft = await AddAircraft("NZZO", DataAuthorityState.CurrentDataAuthority);
        _serverConnection.StationIdentifier.Returns("NZZO");
        await _acarsStationStore.Populate(["NZZO", "YBBB"]);

        var handler = CreateHandler();
        var request = new UpdateNextDataAuthorityRequest(
            Callsign,
            [
                new SectorEntry([MelbourneSector, AustraliaSectorGroup], Now.AddMinutes(20))
            ]);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        await _serverConnection.Received(1).UpdateNextDataAuthority(
            Callsign,
            "YBBB",
            Now.AddMinutes(20),
            Arg.Any<CancellationToken>());

        var info = Assert.IsType<ValidNextDataAuthorityInfo>(aircraft.NextDataAuthorityInfo);
        Assert.Equal("YBBB", info.NextDataAuthority);
    }

    [Fact]
    public async Task Handle_WhenNoCandidatesAreOnline_DoesNotSendNextDataAuthority()
    {
        // Arrange
        var aircraft = await AddAircraft("YBBB", DataAuthorityState.CurrentDataAuthority);
        await _acarsStationStore.Populate(["YBBB"]);

        var handler = CreateHandler();
        var request = new UpdateNextDataAuthorityRequest(
            Callsign,
            [
                new SectorEntry([BrisbaneSector], Now.AddMinutes(10)),
                new SectorEntry([MelbourneSector], Now.AddMinutes(30))
            ]);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        await _serverConnection.DidNotReceiveWithAnyArgs().UpdateNextDataAuthority(
            default!,
            default,
            default,
            default);

        Assert.IsType<NoneNextDataAuthorityInfo>(aircraft.NextDataAuthorityInfo);
    }

    [Fact]
    public async Task Handle_WhenSectorIsClaimedByMultipleAtsuCodes_ReportsError()
    {
        // Arrange
        var aircraft = await AddAircraft("YBBB", DataAuthorityState.CurrentDataAuthority);
        await _acarsStationStore.Populate(["YBBB", "YMMM"]);

        var configuration = CreateConfiguration(
            new AtsuCodeMapping("YBBB", [BrisbaneSector]),
            new AtsuCodeMapping("YMMM", [MelbourneSector]),
            new AtsuCodeMapping("YPPP", [MelbourneSector]));

        var handler = CreateHandler(configuration);
        var request = new UpdateNextDataAuthorityRequest(
            Callsign,
            [
                new SectorEntry([MelbourneSector], Now.AddMinutes(30))
            ]);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        _errorReporter.Received(1).ReportError(Arg.Any<Exception>());
        Assert.IsType<ErrorNextDataAuthorityInfo>(aircraft.NextDataAuthorityInfo);
    }

    [Fact]
    public async Task Handle_WhenNextDataAuthorityDoesNotChange_DoesNotSendAgain()
    {
        // Arrange
        await AddAircraft("YBBB", DataAuthorityState.CurrentDataAuthority);
        await _acarsStationStore.Populate(["YBBB", "YMMM"]);

        var handler = CreateHandler();
        var request = new UpdateNextDataAuthorityRequest(
            Callsign,
            [
                new SectorEntry([MelbourneSector], Now.AddMinutes(30))
            ]);

        // Act
        await handler.Handle(request, CancellationToken.None);
        await handler.Handle(request, CancellationToken.None);

        // Assert
        await _serverConnection.Received(1).UpdateNextDataAuthority(
            Callsign,
            "YMMM",
            Now.AddMinutes(30),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNextDataAuthorityIsNoLongerFound_ClearsNextDataAuthority()
    {
        // Arrange
        var aircraft = await AddAircraft("YBBB", DataAuthorityState.CurrentDataAuthority);
        await _acarsStationStore.Populate(["YBBB", "YMMM"]);

        var handler = CreateHandler();
        await handler.Handle(
            new UpdateNextDataAuthorityRequest(
                Callsign,
                [new SectorEntry([MelbourneSector], Now.AddMinutes(30))]),
            CancellationToken.None);

        // Act
        await handler.Handle(
            new UpdateNextDataAuthorityRequest(Callsign, []),
            CancellationToken.None);

        // Assert
        await _serverConnection.Received(1).UpdateNextDataAuthority(
            Callsign,
            null,
            null,
            Arg.Any<CancellationToken>());

        Assert.IsType<NoneNextDataAuthorityInfo>(aircraft.NextDataAuthorityInfo);
    }

    [Fact]
    public async Task Handle_WhenNotConnected_DoesNothing()
    {
        // Arrange
        var aircraft = await AddAircraft("YBBB", DataAuthorityState.CurrentDataAuthority);
        await _acarsStationStore.Populate(["YBBB", "YMMM"]);
        _serverConnection.IsConnected.Returns(false);

        var handler = CreateHandler();
        var request = new UpdateNextDataAuthorityRequest(
            Callsign,
            [
                new SectorEntry([MelbourneSector], Now.AddMinutes(30))
            ]);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        await _serverConnection.DidNotReceiveWithAnyArgs().UpdateNextDataAuthority(
            default!,
            default,
            default,
            default);

        Assert.IsType<NoneNextDataAuthorityInfo>(aircraft.NextDataAuthorityInfo);
    }

    [Fact]
    public async Task Handle_WhenWeAreNotTheCurrentDataAuthority_DoesNothing()
    {
        // Arrange
        var aircraft = await AddAircraft("YBBB", DataAuthorityState.NextDataAuthority);
        await _acarsStationStore.Populate(["YBBB", "YMMM"]);

        var handler = CreateHandler();
        var request = new UpdateNextDataAuthorityRequest(
            Callsign,
            [
                new SectorEntry([MelbourneSector], Now.AddMinutes(30))
            ]);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        await _serverConnection.DidNotReceiveWithAnyArgs().UpdateNextDataAuthority(
            default!,
            default,
            default,
            default);

        Assert.IsType<NoneNextDataAuthorityInfo>(aircraft.NextDataAuthorityInfo);
    }

    async Task<AircraftConnection> AddAircraft(string stationId, DataAuthorityState dataAuthorityState)
    {
        await _aircraftConnectionStore.Populate([new AircraftConnectionDto(Callsign, stationId, dataAuthorityState)]);
        var connections = await _aircraftConnectionStore.All();
        return connections.Single();
    }

    UpdateNextDataAuthorityRequestHandler CreateHandler(PluginConfiguration? configuration = null)
    {
        return new UpdateNextDataAuthorityRequestHandler(
            _serverConnection,
            _aircraftConnectionStore,
            _acarsStationStore,
            configuration ?? CreateConfiguration(
                new AtsuCodeMapping("YBBB", [BrisbaneSector, AustraliaSectorGroup]),
                new AtsuCodeMapping("YMMM", [MelbourneSector]),
                new AtsuCodeMapping("NZZO", [AucklandSector])),
            _errorReporter,
            Logger.None);
    }

    static PluginConfiguration CreateConfiguration(params AtsuCodeMapping[] atsuCodes)
    {
        return new PluginConfiguration
        {
            ServerEndpoint = "http://localhost",
            Stations = [],
            UplinkMessages = new UplinkMessagesConfiguration
            {
                MasterMessages = [],
                QuickAccessMessages = [],
                Groups = []
            },
            AtsuCodes = atsuCodes
        };
    }
}
