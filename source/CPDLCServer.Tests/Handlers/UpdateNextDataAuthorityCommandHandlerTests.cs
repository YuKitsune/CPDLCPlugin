using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class UpdateNextDataAuthorityCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenAircraftNotFound_DoesNotThrow()
    {
        // Arrange
        var aircraftRepository = new TestAircraftRepository();
        var handler = new UpdateNextDataAuthorityCommandHandler(
            aircraftRepository,
            Logger.None);

        var command = new UpdateNextDataAuthorityCommand(
            "YBBB",
            "UAL123",
            "YMMM",
            DateTimeOffset.UtcNow.AddMinutes(30));

        // Act & Assert - should not throw
        await handler.Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WhenSettingNextDataAuthority_UpdatesAircraftConnection()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new UpdateNextDataAuthorityCommandHandler(
            aircraftRepository,
            Logger.None);

        var expectedTime = clock.UtcNow().AddMinutes(30);
        var command = new UpdateNextDataAuthorityCommand(
            "YBBB",
            "UAL123",
            "YMMM",
            expectedTime);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(aircraft.HasNextDataAuthority);
        Assert.Equal("YMMM", aircraft.NextDataAuthority);
        Assert.Equal(expectedTime, aircraft.ExpectedTransferTime);
        Assert.False(aircraft.DidSentNextDataAuthorityMessage);
    }

    [Fact]
    public async Task Handle_WhenClearingNextDataAuthority_ClearsAircraftConnection()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        aircraft.SetNextDataAuthority("YMMM", clock.UtcNow().AddMinutes(30));
        aircraft.SentNextDataAuthorityMessage();
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new UpdateNextDataAuthorityCommandHandler(
            aircraftRepository,
            Logger.None);

        var command = new UpdateNextDataAuthorityCommand(
            "YBBB",
            "UAL123",
            null,
            null);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(aircraft.HasNextDataAuthority);
        Assert.Null(aircraft.NextDataAuthority);
        Assert.Null(aircraft.ExpectedTransferTime);
        Assert.False(aircraft.DidSentNextDataAuthorityMessage);
    }

    [Fact]
    public async Task Handle_WhenMultipleAircraftWithSameCallsign_UpdatesCorrectOne()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        // Same callsign, different stations
        var aircraftYBBB = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraftYBBB.RequestLogon(clock.UtcNow());
        aircraftYBBB.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraftYBBB.Callsign, aircraftYBBB.AcarsClientId), aircraftYBBB, CancellationToken.None);

        var aircraftYMMM = new AircraftConnection("UAL123", "hoppies-ymmm", "YMMM", DataAuthorityState.NextDataAuthority);
        aircraftYMMM.RequestLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraftYMMM.Callsign, aircraftYMMM.AcarsClientId), aircraftYMMM, CancellationToken.None);

        var handler = new UpdateNextDataAuthorityCommandHandler(
            aircraftRepository,
            Logger.None);

        var expectedTime = clock.UtcNow().AddMinutes(30);
        var command = new UpdateNextDataAuthorityCommand(
            "YBBB",
            "UAL123",
            "NZZO",
            expectedTime);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - only YBBB aircraft should be updated
        Assert.True(aircraftYBBB.HasNextDataAuthority);
        Assert.Equal("NZZO", aircraftYBBB.NextDataAuthority);
        Assert.Equal(expectedTime, aircraftYBBB.ExpectedTransferTime);

        // YMMM aircraft should not be updated
        Assert.False(aircraftYMMM.HasNextDataAuthority);
    }

    [Fact]
    public async Task Handle_WhenUpdatingExistingNextDataAuthority_ReplacesValues()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        aircraft.SetNextDataAuthority("YMMM", clock.UtcNow().AddMinutes(30));
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new UpdateNextDataAuthorityCommandHandler(
            aircraftRepository,
            Logger.None);

        var newTime = clock.UtcNow().AddMinutes(45);
        var command = new UpdateNextDataAuthorityCommand(
            "YBBB",
            "UAL123",
            "NZZO",
            newTime);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - values should be replaced
        Assert.True(aircraft.HasNextDataAuthority);
        Assert.Equal("NZZO", aircraft.NextDataAuthority);
        Assert.Equal(newTime, aircraft.ExpectedTransferTime);
    }

    [Fact]
    public async Task Handle_WhenUpdatingAfterMessageSent_ClearsFlagToAllowNewMessage()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        aircraft.SetNextDataAuthority("YMMM", clock.UtcNow().AddMinutes(30));
        aircraft.SentNextDataAuthorityMessage();
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new UpdateNextDataAuthorityCommandHandler(
            aircraftRepository,
            Logger.None);

        var newTime = clock.UtcNow().AddMinutes(45);
        var command = new UpdateNextDataAuthorityCommand(
            "YBBB",
            "UAL123",
            "NZZO",
            newTime);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - flag should be cleared so a new message can be sent
        Assert.True(aircraft.HasNextDataAuthority);
        Assert.Equal("NZZO", aircraft.NextDataAuthority);
        Assert.Equal(newTime, aircraft.ExpectedTransferTime);
        Assert.False(aircraft.DidSentNextDataAuthorityMessage);
    }
}
