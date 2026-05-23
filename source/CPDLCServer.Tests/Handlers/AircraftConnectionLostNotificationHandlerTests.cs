using CPDLCServer.Handlers;
using CPDLCServer.Hubs;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class AircraftConnectionLostNotificationHandlerTests
{
    [Fact]
    public async Task Handle_RemovesAircraftFromTracking()
    {
        // Arrange
        var aircraftManager = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(DateTimeOffset.UtcNow);
        aircraft.AcceptLogon(DateTimeOffset.UtcNow);
        await aircraftManager.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var controllerManager = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var publisher = new TestPublisher();
        var clock = new TestClock();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftConnectionLostNotificationHandler(
            aircraftManager,
            controllerManager,
            dialogueRepository,
            hubContext,
            messageIdProvider,
            publisher,
            clock,
            Logger.None);

        var notification = new AircraftConnectionLost("hoppies-ybbb", "UAL123");

        // Assert - aircraft exists before handling
        Assert.NotNull(await aircraftManager.Find(new("UAL123", "hoppies-ybbb"), CancellationToken.None));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - aircraft is removed after handling
        Assert.Null(await aircraftManager.Find(new("UAL123", "hoppies-ybbb"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CreatesDialogueWithErrorMessageAndNotifiesControllers()
    {
        // Arrange
        var aircraftManager = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(DateTimeOffset.UtcNow);
        aircraft.AcceptLogon(DateTimeOffset.UtcNow);
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

        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var publisher = new TestPublisher();
        var clock = new TestClock();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftConnectionLostNotificationHandler(
            aircraftManager,
            controllerManager,
            dialogueRepository,
            hubContext,
            messageIdProvider,
            publisher,
            clock,
            Logger.None);

        var notification = new AircraftConnectionLost("hoppies-ybbb", "UAL123");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - controllers are notified once for AircraftDisconnected
        hubContext.Clients.Received(1).Clients(
            Arg.Is<IReadOnlyList<string>>(ids =>
                ids.Count == 2 &&
                ids.Contains("ConnectionId-1") &&
                ids.Contains("ConnectionId-2")));

        // Assert - disconnection notification is sent with callsign and stationId
        await clientProxy.Received(1).SendCoreAsync(
            "AircraftConnectionRemoved",
            Arg.Is<object[]>(args => args.Length == 2 && args[0].ToString() == "UAL123" && args[1].ToString() == "YBBB"),
            Arg.Any<CancellationToken>());

        // Assert - DialogueChangedNotification is published
        Assert.Single(publisher.PublishedNotifications.OfType<DialogueChangedNotification>());
        var dialogueNotification = publisher.PublishedNotifications.OfType<DialogueChangedNotification>().First();
        Assert.Equal("UAL123", dialogueNotification.Dialogue.AircraftCallsign);
        Assert.Single(dialogueNotification.Dialogue.Messages);

        var errorMessage = dialogueNotification.Dialogue.Messages.First() as DownlinkMessage;
        Assert.NotNull(errorMessage);
        Assert.Equal("ERROR CONNECTION TIMED OUT", errorMessage.Content);
        Assert.Equal(AlertType.Medium, errorMessage.AlertType);
        Assert.Equal("UAL123", errorMessage.Sender);
    }

    [Fact]
    public async Task Handle_DoesNotThrowWhenAircraftNotFound()
    {
        // Arrange
        var aircraftManager = new TestAircraftRepository();
        var controllerManager = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var publisher = new TestPublisher();
        var clock = new TestClock();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();

        var handler = new AircraftConnectionLostNotificationHandler(
            aircraftManager,
            controllerManager,
            dialogueRepository,
            hubContext,
            messageIdProvider,
            publisher,
            clock,
            Logger.None);

        var notification = new AircraftConnectionLost("hoppies-ybbb", "UAL123");

        // Act & Assert - should not throw
        await handler.Handle(notification, CancellationToken.None);

        // Assert - no SignalR calls should be made
        hubContext.Clients.DidNotReceive().Clients(Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task Handle_DoesNotNotifyWhenNoControllersConnected()
    {
        // Arrange
        var aircraftManager = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(DateTimeOffset.UtcNow);
        aircraft.AcceptLogon(DateTimeOffset.UtcNow);
        await aircraftManager.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var controllerManager = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var publisher = new TestPublisher();
        var clock = new TestClock();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftConnectionLostNotificationHandler(
            aircraftManager,
            controllerManager,
            dialogueRepository,
            hubContext,
            messageIdProvider,
            publisher,
            clock,
            Logger.None);

        var notification = new AircraftConnectionLost("hoppies-ybbb", "UAL123");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - aircraft should still be removed
        Assert.Null(await aircraftManager.Find(new("UAL123", "hoppies-ybbb"), CancellationToken.None));

        // Assert - no SignalR notification should be sent
        hubContext.Clients.DidNotReceive().Clients(Arg.Any<IReadOnlyList<string>>());
        await clientProxy.DidNotReceive().SendCoreAsync(
            Arg.Any<string>(),
            Arg.Any<object[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AddsErrorMessageToExistingOpenDialogue()
    {
        // Arrange
        var aircraftManager = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(DateTimeOffset.UtcNow);
        aircraft.AcceptLogon(DateTimeOffset.UtcNow);
        await aircraftManager.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var controllerManager = new TestControllerRepository();
        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-1",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        await controllerManager.Add(controller, CancellationToken.None);

        var dialogueRepository = new TestDialogueRepository();
        var clock = new TestClock();

        // Create an existing dialogue with an open downlink message
        var existingDialogue = new Dialogue("UAL123");
        existingDialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST DESCENT TO FL350",
            clock.UtcNow());
        await dialogueRepository.Add(existingDialogue, CancellationToken.None);

        var messageIdProvider = new TestMessageIdProvider();
        messageIdProvider.SetNextId(2); // Next ID will be 2
        var publisher = new TestPublisher();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftConnectionLostNotificationHandler(
            aircraftManager,
            controllerManager,
            dialogueRepository,
            hubContext,
            messageIdProvider,
            publisher,
            clock,
            Logger.None);

        var notification = new AircraftConnectionLost("hoppies-ybbb", "UAL123");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - DialogueChangedNotification is published
        Assert.Single(publisher.PublishedNotifications.OfType<DialogueChangedNotification>());
        var dialogueNotification = publisher.PublishedNotifications.OfType<DialogueChangedNotification>().First();

        // Assert - error message was added to the existing dialogue
        Assert.Equal(2, dialogueNotification.Dialogue.Messages.Count);
        var errorMessage = dialogueNotification.Dialogue.Messages.Last() as DownlinkMessage;
        Assert.NotNull(errorMessage);
        Assert.Equal("ERROR CONNECTION TIMED OUT", errorMessage.Content);
        Assert.Equal(AlertType.Medium, errorMessage.AlertType);
        Assert.Equal(1, errorMessage.MessageReference); // References the open downlink
    }
}
