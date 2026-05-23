using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class TerminateConnectionOnDialogueClosedHandlerTests
{
    [Fact]
    public async Task Handle_ClosedDialogueWithEndService_TerminatesConnection()
    {
        // Arrange
        var aircraftRepository = new TestAircraftRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();

        // Create aircraft connection
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new TerminateConnectionOnDialogueClosedHandler(
            aircraftRepository,
            mediator,
            Logger.None);

        // Create a closed dialogue with END SERVICE
        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "END SERVICE",
            clock.UtcNow());

        var notification = new DialogueChangedNotification(dialogue);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mediator.Received(1)
            .Send(
                Arg.Is<TerminateConnectionRequest>(req =>
                    req.Callsign == "UAL123" &&
                    req.AcarsClientId == "hoppies-ybbb"),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OpenDialogueWithEndService_DoesNotTerminateConnection()
    {
        // Arrange
        var aircraftRepository = new TestAircraftRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();

        // Create aircraft connection
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new TerminateConnectionOnDialogueClosedHandler(
            aircraftRepository,
            mediator,
            Logger.None);

        // Create an open dialogue with END SERVICE (requires response)
        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "END SERVICE. CONTACT AUCKLAND ON 123.450.",
            clock.UtcNow());

        var notification = new DialogueChangedNotification(dialogue);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - should NOT terminate because dialogue is still open
        await mediator.DidNotReceive()
            .Send(
                Arg.Any<TerminateConnectionRequest>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ClosedDialogueWithoutEndService_DoesNotTerminateConnection()
    {
        // Arrange
        var aircraftRepository = new TestAircraftRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();

        // Create aircraft connection
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new TerminateConnectionOnDialogueClosedHandler(
            aircraftRepository,
            mediator,
            Logger.None);

        // Create a closed dialogue WITHOUT END SERVICE
        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "LOGON ACCEPTED",
            clock.UtcNow());

        var notification = new DialogueChangedNotification(dialogue);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - should NOT terminate because no END SERVICE
        await mediator.DidNotReceive()
            .Send(
                Arg.Any<TerminateConnectionRequest>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ClosedDialogueWithMultipleMessages_TerminatesWhenEndServicePresent()
    {
        // Arrange
        var aircraftRepository = new TestAircraftRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();

        // Create aircraft connection
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new TerminateConnectionOnDialogueClosedHandler(
            aircraftRepository,
            mediator,
            Logger.None);

        // Create a dialogue with multiple messages including END SERVICE
        var dialogue = new Dialogue("UAL123");

        // Pilot request
        dialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB TO FL370",
            clock.UtcNow());

        // Controller response with END SERVICE
        dialogue.AddUplink(
            2,
            1,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLEARED TO CLIMB TO FL370. CONTACT MELBOURNE 123.45. END SERVICE",
            clock.UtcNow());

        // Pilot response (closes the dialogue)
        dialogue.AddDownlink(
            3,
            2,
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse,
            AlertType.None,
            "WILCO",
            clock.UtcNow());

        var notification = new DialogueChangedNotification(dialogue);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - should terminate because dialogue is closed and contains END SERVICE
        await mediator.Received(1)
            .Send(
                Arg.Is<TerminateConnectionRequest>(req =>
                    req.Callsign == "UAL123" &&
                    req.AcarsClientId == "hoppies-ybbb"),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AircraftConnectionNotFound_LogsWarningAndDoesNotCrash()
    {
        // Arrange
        var aircraftRepository = new TestAircraftRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();

        // Note: NOT adding aircraft to repository

        var handler = new TerminateConnectionOnDialogueClosedHandler(
            aircraftRepository,
            mediator,
            Logger.None);

        // Create a closed dialogue with END SERVICE
        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "END SERVICE",
            clock.UtcNow());

        var notification = new DialogueChangedNotification(dialogue);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - should NOT crash and should NOT send terminate request
        await mediator.DidNotReceive()
            .Send(
                Arg.Any<TerminateConnectionRequest>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AircraftConnectionNotCDA_DoesNotTerminate()
    {
        // Arrange
        var aircraftRepository = new TestAircraftRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();

        // Create aircraft connection as NDA (Next Data Authority), not CDA
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.NextDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new TerminateConnectionOnDialogueClosedHandler(
            aircraftRepository,
            mediator,
            Logger.None);

        // Create a closed dialogue with END SERVICE
        var dialogue = new Dialogue("UAL123");
        dialogue.AddUplink(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "END SERVICE",
            clock.UtcNow());

        var notification = new DialogueChangedNotification(dialogue);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - should NOT terminate because aircraft is NDA, not CDA
        await mediator.DidNotReceive()
            .Send(
                Arg.Any<TerminateConnectionRequest>(),
                Arg.Any<CancellationToken>());
    }
}
