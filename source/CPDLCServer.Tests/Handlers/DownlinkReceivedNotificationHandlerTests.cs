using CPDLCServer.Handlers;
using CPDLCServer.Hubs;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class DownlinkReceivedNotificationHandlerTests
{
    [Fact]
    public async Task Handle_PublishesDialogueChangedNotification()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftManager = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftManager.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var controllerManager = new TestControllerRepository();
        var controller1 = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-1",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        var controller2 = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-2",
            "YBBB",
            "BN-OCN_CTR",
            "7654321");
        await controllerManager.Add(controller1, CancellationToken.None);
        await controllerManager.Add(controller2, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var dialogueRepository = new TestDialogueRepository();

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            dialogueRepository,
            hubContext,
            Logger.None);

        var downlink = new ReceivedDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST DESCENT",
            clock.UtcNow());

        var notification = new DownlinkReceivedNotification(
            "hoppies-ybbb",
            "YBBB",
            downlink);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - DialogueChangedNotification is published via mediator
        await mediator.Received(1).Publish(Arg.Any<DialogueChangedNotification>(), Arg.Any<CancellationToken>());

        var dialogues = await dialogueRepository.All(CancellationToken.None);
        Assert.Single(dialogues);
        Assert.Equal("UAL123", dialogues[0].AircraftCallsign);
        Assert.Single(dialogues[0].Messages);
        var msg = Assert.IsType<DownlinkMessage>(dialogues[0].Messages[0]);
        Assert.Equal("REQUEST DESCENT", msg.Content);
    }

    [Fact]
    public async Task Handle_StillCreatesDialogueWhenNoControllersMatch()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftManager = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftManager.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var controllerManager = new TestControllerRepository();
        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-1",
            "YMMM",
            "ML-IND_FSS",
            "1234567");
        await controllerManager.Add(controller, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var dialogueRepository = new TestDialogueRepository();

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            dialogueRepository,
            hubContext,
            Logger.None);

        var downlink = new ReceivedDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST DESCENT",
            clock.UtcNow());

        var notification = new DownlinkReceivedNotification(
            "hoppies-ybbb",
            "YBBB",
            downlink);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - dialogue is still created even with no matching controllers
        await mediator.Received(1).Publish(Arg.Any<DialogueChangedNotification>(), Arg.Any<CancellationToken>());
        var dialogues = await dialogueRepository.All(CancellationToken.None);
        Assert.Single(dialogues);
        Assert.Equal("UAL123", dialogues[0].AircraftCallsign);
    }

    [Fact]
    public async Task Handle_PromotesAircraftToCurrentDataAuthorityOnFirstDownlink()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftManager = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.NextDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftManager.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var controllerManager = new TestControllerRepository();
        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-1",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        await controllerManager.Add(controller, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var dialogueRepository = new TestDialogueRepository();

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            dialogueRepository,
            hubContext,
            Logger.None);

        var downlink = new ReceivedDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST DESCENT",
            clock.UtcNow());

        var notification = new DownlinkReceivedNotification(
            "hoppies-ybbb",
            "YBBB",
            downlink);

        // Assert - aircraft starts as NextDataAuthority
        Assert.Equal(DataAuthorityState.NextDataAuthority, aircraft.DataAuthorityState);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - aircraft is promoted to CurrentDataAuthority
        Assert.Equal(DataAuthorityState.CurrentDataAuthority, aircraft.DataAuthorityState);

        // Assert - AircraftConnectionUpdated event was sent to controllers
        var receivedCalls = clientProxy.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == "SendCoreAsync")
            .ToList();

        Assert.Single(receivedCalls);
        var args = receivedCalls[0].GetArguments();
        Assert.Equal("AircraftConnectionUpdated", args[0]);

        var eventArgs = args[1] as object[];
        Assert.NotNull(eventArgs);
        Assert.Single(eventArgs);

        var dto = eventArgs[0] as Contracts.AircraftConnectionDto;
        Assert.NotNull(dto);
        Assert.Equal("UAL123", dto.Callsign);
        Assert.Equal("YBBB", dto.StationId);
        Assert.Equal(Contracts.DataAuthorityState.CurrentDataAuthority, dto.DataAuthorityState);
    }

    [Fact]
    public async Task Handle_DoesNotPromoteToCurrentDataAuthority_WhenNotCurrentDataAuthorityMessageReceived()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftManager = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.NextDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftManager.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var controllerManager = new TestControllerRepository();
        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-1",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        await controllerManager.Add(controller, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var dialogueRepository = new TestDialogueRepository();

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            dialogueRepository,
            hubContext,
            Logger.None);

        var downlink = new ReceivedDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse,
            AlertType.None,
            "NOT CURRENT DATA AUTHORITY",
            clock.UtcNow());

        var notification = new DownlinkReceivedNotification(
            "hoppies-ybbb",
            "YBBB",
            downlink);

        // Assert - aircraft starts as NextDataAuthority
        Assert.Equal(DataAuthorityState.NextDataAuthority, aircraft.DataAuthorityState);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - aircraft remains as NextDataAuthority
        Assert.Equal(DataAuthorityState.NextDataAuthority, aircraft.DataAuthorityState);

        // Assert - AircraftConnectionUpdated event was NOT sent
        var receivedCalls = clientProxy.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == "SendCoreAsync")
            .ToList();

        Assert.Empty(receivedCalls);
    }

    [Fact]
    public async Task Handle_UpdatesLastSeen()
    {
        // Arrange
        var logonTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var clock = new TestClock();
        clock.SetUtcNow(logonTime);

        var aircraftManager = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.NextDataAuthority);
        aircraft.RequestLogon(logonTime);
        aircraft.AcceptLogon(logonTime);
        await aircraftManager.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var controllerManager = new TestControllerRepository();
        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-1",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        await controllerManager.Add(controller, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var expectedLastSeen = new DateTimeOffset(2026, 1, 1, 1, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(expectedLastSeen);

        var dialogueRepository = new TestDialogueRepository();

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            dialogueRepository,
            hubContext,
            Logger.None);

        var downlink = new ReceivedDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST DESCENT",
            clock.UtcNow());

        var notification = new DownlinkReceivedNotification(
            "hoppies-ybbb",
            "YBBB",
            downlink);

        // Assert
        Assert.Equal(logonTime, aircraft.LastSeen);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Equal(expectedLastSeen, aircraft.LastSeen);
    }

    [Fact]
    public async Task Handle_CreatesNewDialogue_ForDownlinkWithNoReference()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftRepository,
            mediator,
            clock,
            controllerRepository,
            dialogueRepository,
            hubContext,
            Logger.None);

        var downlink = new ReceivedDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            clock.UtcNow());

        var notification = new DownlinkReceivedNotification("hoppies-ybbb", "YBBB", downlink);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - a new dialogue was created with the downlink
        var dialogues = await dialogueRepository.All(CancellationToken.None);
        Assert.Single(dialogues);
        Assert.Equal("UAL123", dialogues[0].AircraftCallsign);
        Assert.Single(dialogues[0].Messages);
        var msg = Assert.IsType<DownlinkMessage>(dialogues[0].Messages[0]);
        Assert.Equal("REQUEST CLIMB FL410", msg.Content);
        Assert.Equal(1, msg.MessageId);
    }

    [Fact]
    public async Task Handle_AppendsToExistingDialogue_ForDownlinkWithReference()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();

        // Create existing dialogue with an uplink
        var existingDialogue = new Dialogue("UAL123");
        existingDialogue.AddUplink(
            5,
            null,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLIMB TO FL410",
            clock.UtcNow());
        await dialogueRepository.Add(existingDialogue, CancellationToken.None);

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftRepository,
            mediator,
            clock,
            controllerRepository,
            dialogueRepository,
            hubContext,
            Logger.None);

        var downlink = new ReceivedDownlink(
            10,
            5,
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse,
            AlertType.None,
            "WILCO",
            clock.UtcNow().AddSeconds(10));

        var notification = new DownlinkReceivedNotification("hoppies-ybbb", "YBBB", downlink);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - downlink appended to the existing dialogue (use All since WILCO closes the dialogue)
        var allDialogues = await dialogueRepository.All(CancellationToken.None);
        Assert.Single(allDialogues);
        Assert.Equal(2, allDialogues[0].Messages.Count);
        var appendedMsg = allDialogues[0].Messages.OfType<DownlinkMessage>().FirstOrDefault(m => m.MessageId == 10);
        Assert.NotNull(appendedMsg);
        Assert.Equal("WILCO", appendedMsg.Content);
    }

    [Fact]
    public async Task Handle_FallsBackToNewDialogue_WhenNoOpenDialogueMatchesReference()
    {
        // Branch 3 fallback: aircraft sends a reply referencing an uplink that no longer has an
        // open dialogue (e.g. controller manually closed it). The handler must not drop the
        // message - it creates a new dialogue instead.
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftRepository, mediator, clock, controllerRepository, dialogueRepository, hubContext, Logger.None);

        // Downlink has a MessageReference (uplink ID=99) but no open dialogue exists for it
        var downlink = new ReceivedDownlink(
            10,
            99, // References uplink that doesn't exist in any open dialogue
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse,
            AlertType.None,
            "WILCO",
            clock.UtcNow());

        var notification = new DownlinkReceivedNotification("hoppies-ybbb", "YBBB", downlink);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - a new dialogue was created as fallback (message is not dropped)
        var dialogues = await dialogueRepository.All(CancellationToken.None);
        Assert.Single(dialogues);
        Assert.Equal("UAL123", dialogues[0].AircraftCallsign);
        Assert.Single(dialogues[0].Messages);
        var msg = Assert.IsType<DownlinkMessage>(dialogues[0].Messages[0]);
        Assert.Equal(10, msg.MessageId);
        Assert.Equal(99, msg.MessageReference);

        await mediator.Received(1).Publish(Arg.Any<DialogueChangedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotAppendToClosedDialogue_WhenAircraftReusesMessageId()
    {
        // Regression: MessageId is scoped per ACARS session. If an aircraft re-uses a downlink
        // MessageId from a previous (now closed) session, FindOpenDialogueByUplink must not
        // match the closed dialogue - a new dialogue should be created instead.
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();

        // Prior session: uplink ID=5 was sent and fully replied to (dialogue is now closed)
        var priorDialogue = new Dialogue("UAL123");
        priorDialogue.AddUplink(
            5, null, "UAL123", "YBBB",
            CpdlcUplinkResponseType.WilcoUnable, AlertType.None, "CLIMB FL410",
            clock.UtcNow());
        priorDialogue.AddDownlink(
            3, 5, "UAL123",
            CpdlcDownlinkResponseType.NoResponse, AlertType.None, "WILCO",
            clock.UtcNow().AddSeconds(30));
        await dialogueRepository.Add(priorDialogue, CancellationToken.None);

        Assert.True(priorDialogue.IsClosed);

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftRepository, mediator, clock, controllerRepository, dialogueRepository, hubContext, Logger.None);

        // New session: aircraft re-uses MessageReference=5 (same uplink ID as in the old session)
        var downlink = new ReceivedDownlink(
            10,
            5, // Same MessageReference as old session's uplink
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse,
            AlertType.None,
            "WILCO",
            clock.UtcNow().AddMinutes(30));

        var notification = new DownlinkReceivedNotification("hoppies-ybbb", "YBBB", downlink);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - a NEW dialogue was created (closed dialogue was not reopened or appended to)
        var allDialogues = await dialogueRepository.All(CancellationToken.None);
        Assert.Equal(2, allDialogues.Length);

        var newDialogue = allDialogues.First(d => d.Id != priorDialogue.Id);
        Assert.Single(newDialogue.Messages); // Only the new downlink
        Assert.Equal(2, priorDialogue.Messages.Count); // Prior dialogue is unchanged
    }

    [Fact]
    public async Task Handle_LogoffMessage_TerminatesConnectionAndCreatesDialogue()
    {
        // Logoff messages must: (1) trigger connection termination, (2) still flow through
        // to create a dialogue so the controller sees the logoff notification.
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftRepository, mediator, clock, controllerRepository, dialogueRepository, hubContext, Logger.None);

        var downlink = new ReceivedDownlink(
            1, null, "UAL123",
            CpdlcDownlinkResponseType.NoResponse, AlertType.None,
            "LOGOFF",
            clock.UtcNow());

        var notification = new DownlinkReceivedNotification("hoppies-ybbb", "YBBB", downlink);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - connection termination was requested
        await mediator.Received(1).Send(
            Arg.Is<TerminateConnectionRequest>(r => r.Callsign == "UAL123" && r.AcarsClientId == "hoppies-ybbb"),
            Arg.Any<CancellationToken>());

        // Assert - logoff message still created a dialogue for the controller to see
        var dialogues = await dialogueRepository.All(CancellationToken.None);
        Assert.Single(dialogues);
        var msg = Assert.IsType<DownlinkMessage>(dialogues[0].Messages[0]);
        Assert.Equal("LOGOFF", msg.Content);

        await mediator.Received(1).Publish(Arg.Any<DialogueChangedNotification>(), Arg.Any<CancellationToken>());
    }
}
