using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class LogonCommandHandlerTests
{
    static LogonCommand BuildLogonCommand(TestClock clock) => new(
        DownlinkId: 1,
        DownlinkMessageReference: null,
        Callsign: "QFA1",
        DownlinkResponseType: CpdlcDownlinkResponseType.NoResponse,
        DownlinkAlertType: AlertType.None,
        DownlinkContent: "REQUEST LOGON",
        DownlinkReceived: clock.UtcNow(),
        AcarsClientId: "hoppies-ybbb");

    static LogonCommandHandler BuildHandler(
        TestClientManager clientManager,
        TestAircraftRepository aircraftRepository,
        TestControllerRepository controllerRepository,
        TestDialogueRepository dialogueRepository,
        TestMessageIdProvider messageIdProvider,
        TestClock clock,
        IMediator mediator) =>
        new(
            clientManager,
            aircraftRepository,
            controllerRepository,
            dialogueRepository,
            messageIdProvider,
            clock,
            mediator,
            Logger.None);

    [Fact]
    public async Task Handle_NoATC_RejectsLogon()
    {
        // Arrange
        var clock = new TestClock();
        var clientManager = new TestClientManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var mediator = Substitute.For<IMediator>();

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(clientManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - rejection uplink transmitted via the ACARS client
        var client = (TestAcarsClient)await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        Assert.Single(client.SentMessages);
        Assert.Equal("LOGON REJECTED. NO ATS AVBL.", client.SentMessages[0].Content);
        Assert.Equal("QFA1", client.SentMessages[0].Recipient);
        Assert.Equal(1, client.SentMessages[0].MessageReference);
    }

    [Fact]
    public async Task Handle_NoATC_DoesNotTrackAircraft()
    {
        // Arrange
        var clock = new TestClock();
        var clientManager = new TestClientManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var mediator = Substitute.For<IMediator>();

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(clientManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - the in-flight aircraft entry is rolled back
        var tracked = await aircraftRepository.All(CancellationToken.None);
        Assert.Empty(tracked);
    }

    [Fact]
    public async Task Handle_NewConnection_AcceptsLogon()
    {
        // Arrange
        var clock = new TestClock();
        var clientManager = new TestClientManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var mediator = Substitute.For<IMediator>();

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "Connection-1",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        await controllerRepository.Add(controller, CancellationToken.None);

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(clientManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - acceptance uplink transmitted
        var client = (TestAcarsClient)await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        Assert.Single(client.SentMessages);
        Assert.Equal("LOGON ACCEPTED", client.SentMessages[0].Content);
        Assert.Equal("QFA1", client.SentMessages[0].Recipient);
        Assert.Equal(1, client.SentMessages[0].MessageReference);
    }

    [Fact]
    public async Task Handle_NewConnection_TracksConnection()
    {
        // Arrange
        var clock = new TestClock();
        var clientManager = new TestClientManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var mediator = Substitute.For<IMediator>();

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "Connection-1",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        await controllerRepository.Add(controller, CancellationToken.None);

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(clientManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var trackedAircraft = await aircraftRepository.Find(new("QFA1", "hoppies-ybbb"), CancellationToken.None);
        Assert.NotNull(trackedAircraft);
        Assert.Equal(DataAuthorityState.NextDataAuthority, trackedAircraft.DataAuthorityState);
        Assert.Equal(clock.UtcNow(), trackedAircraft.LogonRequested);
        Assert.Equal(clock.UtcNow(), trackedAircraft.LogonAccepted);
        Assert.Equal(clock.UtcNow(), trackedAircraft.LastSeen);
    }

    [Fact]
    public async Task Handle_NewConnection_PersistsDialogueWithDownlinkAndUplink()
    {
        // Arrange
        var clock = new TestClock();
        var clientManager = new TestClientManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var mediator = Substitute.For<IMediator>();

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "Connection-1",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        await controllerRepository.Add(controller, CancellationToken.None);

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(clientManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - dialogue contains the original LOGON REQUEST downlink and the LOGON ACCEPTED uplink
        var dialogues = await dialogueRepository.All(CancellationToken.None);
        Assert.Single(dialogues);
        Assert.Equal("QFA1", dialogues[0].AircraftCallsign);
        Assert.Equal(2, dialogues[0].Messages.Count);

        var downlink = Assert.IsType<DownlinkMessage>(dialogues[0].Messages[0]);
        Assert.Equal("REQUEST LOGON", downlink.Content);
        Assert.Equal(1, downlink.MessageId);

        var uplink = Assert.IsType<UplinkMessage>(dialogues[0].Messages[1]);
        Assert.Equal("LOGON ACCEPTED", uplink.Content);
        Assert.Equal(1, uplink.MessageReference);

        await mediator.Received(1).Publish(Arg.Any<DialogueChangedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingConnection_AcceptsLogon_AndDoesNotDuplicateConnection()
    {
        // Arrange
        var clock = new TestClock();
        var clientManager = new TestClientManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var mediator = Substitute.For<IMediator>();

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "Connection-1",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        await controllerRepository.Add(controller, CancellationToken.None);

        var existing = new AircraftConnection("QFA1", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        existing.RequestLogon(clock.UtcNow());
        existing.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new("QFA1", "hoppies-ybbb"), existing, CancellationToken.None);

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(clientManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var client = (TestAcarsClient)await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        Assert.Single(client.SentMessages);
        Assert.Equal("LOGON ACCEPTED", client.SentMessages[0].Content);

        var allTrackedAircraft = await aircraftRepository.All(CancellationToken.None);
        Assert.Single(allTrackedAircraft);
        Assert.Equal("QFA1", allTrackedAircraft.Single().Callsign);
    }

    [Fact]
    public async Task Handle_NotifiesATC()
    {
        // Arrange
        var clock = new TestClock();
        var clientManager = new TestClientManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var mediator = Substitute.For<IMediator>();

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "Connection-1",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        await controllerRepository.Add(controller, CancellationToken.None);

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(clientManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(new AircraftConnectionEstablished(
                "hoppies-ybbb",
                "YBBB",
                "QFA1",
                DataAuthorityState.NextDataAuthority),
            Arg.Any<CancellationToken>());
    }
}
