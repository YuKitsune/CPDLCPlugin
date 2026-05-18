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

        var publisher = new TestPublisher();
        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            dialogueRepository,
            hubContext,
            publisher,
            Logger.None);

        var downlinkMessage = new DownlinkMessage(
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
            downlinkMessage);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - DialogueChangedNotification is published
        Assert.Single(publisher.PublishedNotifications.OfType<DialogueChangedNotification>());
        var dialogueNotification = publisher.PublishedNotifications.OfType<DialogueChangedNotification>().First();
        Assert.Equal("UAL123", dialogueNotification.Dialogue.AircraftCallsign);
        Assert.Single(dialogueNotification.Dialogue.Messages);
        Assert.Equal(downlinkMessage, dialogueNotification.Dialogue.Messages.First());
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

        var publisher = new TestPublisher();
        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            dialogueRepository,
            hubContext,
            publisher,
            Logger.None);

        var downlinkMessage = new DownlinkMessage(
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
            downlinkMessage);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - DialogueChangedNotification is still published even with no matching controllers
        Assert.Single(publisher.PublishedNotifications.OfType<DialogueChangedNotification>());
        var dialogueNotification = publisher.PublishedNotifications.OfType<DialogueChangedNotification>().First();
        Assert.Equal("UAL123", dialogueNotification.Dialogue.AircraftCallsign);
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

        var publisher = new TestPublisher();
        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            dialogueRepository,
            hubContext,
            publisher,
            Logger.None);

        var downlinkMessage = new DownlinkMessage(
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
            downlinkMessage);

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

        var publisher = new TestPublisher();
        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            dialogueRepository,
            hubContext,
            publisher,
            Logger.None);

        var downlinkMessage = new DownlinkMessage(
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
            downlinkMessage);

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

        var publisher = new TestPublisher();
        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            dialogueRepository,
            hubContext,
            publisher,
            Logger.None);

        var downlinkMessage = new DownlinkMessage(
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
            downlinkMessage);

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

        var publisher = new TestPublisher();
        var handler = new DownlinkReceivedNotificationHandler(
            aircraftRepository,
            mediator,
            clock,
            controllerRepository,
            dialogueRepository,
            hubContext,
            publisher,
            Logger.None);

        var downlink = new DownlinkMessage(
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

        // Assert
        var dialogue = await dialogueRepository.FindOpenDialogueByUplink(
            "UAL123",
            1,
            CancellationToken.None);

        Assert.NotNull(dialogue);
        Assert.Single(dialogue.Messages);
        Assert.Equal(downlink, dialogue.Messages[0]);
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
        var uplink = new UplinkMessage(
            5,
            null,
            "UAL123",
            "SYSTEM",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLIMB TO FL410",
            clock.UtcNow());

        var existingDialogue = new Dialogue("UAL123", uplink);
        await dialogueRepository.Add(existingDialogue, CancellationToken.None);

        var publisher = new TestPublisher();
        var handler = new DownlinkReceivedNotificationHandler(
            aircraftRepository,
            mediator,
            clock,
            controllerRepository,
            dialogueRepository,
            hubContext,
            publisher,
            Logger.None);

        var downlink = new DownlinkMessage(
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

        // Assert
        var dialogue = await dialogueRepository.FindOpenDialogueByUplink(
            "UAL123",
            5,
            CancellationToken.None);

        Assert.NotNull(dialogue);
        Assert.Equal(2, dialogue.Messages.Count);
        Assert.Contains(downlink, dialogue.Messages);
    }
}
