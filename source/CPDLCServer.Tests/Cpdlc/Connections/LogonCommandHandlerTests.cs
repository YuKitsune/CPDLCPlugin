using CPDLCServer.Controllers;
using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Cpdlc.Connections;

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
        AcarsConnectionId: "hoppies-ybbb");

    static LogonCommandHandler BuildHandler(
        TestAcarsConnectionManager acarsConnectionManager,
        TestAircraftRepository aircraftRepository,
        TestControllerRepository controllerRepository,
        TestDialogueRepository dialogueRepository,
        TestMessageIdProvider messageIdProvider,
        TestClock clock,
        IMediator mediator) =>
        new(
            acarsConnectionManager,
            aircraftRepository,
            controllerRepository,
            dialogueRepository,
            messageIdProvider,
            clock,
            mediator,
            new TestUplinkTransmitter(),
            Logger.None);

    [Fact]
    public async Task Handle_NoATC_RejectsLogon()
    {
        // Arrange
        var clock = new TestClock();
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var mediator = Substitute.For<IMediator>();

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(acarsConnectionManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - rejection uplink transmitted via the ACARS connection
        var acarsConnection = (TestAcarsConnection)await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        Assert.Single(acarsConnection.SentMessages);
        Assert.Equal("LOGON REJECTED. NO ATS AVBL.", acarsConnection.SentMessages[0].Content);
        Assert.Equal("QFA1", acarsConnection.SentMessages[0].Recipient);
        Assert.Equal(1, acarsConnection.SentMessages[0].MessageReference);
    }

    [Fact]
    public async Task Handle_NoATC_DoesNotTrackAircraft()
    {
        // Arrange
        var clock = new TestClock();
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var mediator = Substitute.For<IMediator>();

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(acarsConnectionManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

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
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var mediator = Substitute.For<IMediator>();

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "Connection-1",
            "YBBB",
            "BN-TSN_FSS");
        await controllerRepository.Add(controller, CancellationToken.None);

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(acarsConnectionManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - acceptance uplink transmitted
        var acarsConnection = (TestAcarsConnection)await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        Assert.Single(acarsConnection.SentMessages);
        Assert.Equal("LOGON ACCEPTED", acarsConnection.SentMessages[0].Content);
        Assert.Equal("QFA1", acarsConnection.SentMessages[0].Recipient);
        Assert.Equal(1, acarsConnection.SentMessages[0].MessageReference);
    }

    [Fact]
    public async Task Handle_NewConnection_TracksConnection()
    {
        // Arrange
        var clock = new TestClock();
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var mediator = Substitute.For<IMediator>();

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "Connection-1",
            "YBBB",
            "BN-TSN_FSS");
        await controllerRepository.Add(controller, CancellationToken.None);

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(acarsConnectionManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

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
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var mediator = Substitute.For<IMediator>();

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "Connection-1",
            "YBBB",
            "BN-TSN_FSS");
        await controllerRepository.Add(controller, CancellationToken.None);

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(acarsConnectionManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

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

    static async Task<AircraftConnection> AddExistingConnection(TestAircraftRepository aircraftRepository, TestClock clock)
    {
        var existing = new AircraftConnection("QFA1", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        existing.RequestLogon(clock.UtcNow());
        existing.AcceptLogon(clock.UtcNow());
        existing.SetNextDataAuthority("YMMM", clock.UtcNow().AddMinutes(30));
        await aircraftRepository.Add(new("QFA1", "hoppies-ybbb"), existing, CancellationToken.None);
        return existing;
    }

    static async Task AddController(TestControllerRepository controllerRepository)
    {
        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "Connection-1",
            "YBBB",
            "BN-TSN_FSS");
        await controllerRepository.Add(controller, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ExistingConnection_ReplacesConnection()
    {
        // Arrange
        var clock = new TestClock();
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var mediator = Substitute.For<IMediator>();

        await AddController(controllerRepository);
        var existing = await AddExistingConnection(aircraftRepository, clock);

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(acarsConnectionManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - the logon is accepted
        var acarsConnection = (TestAcarsConnection)await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        Assert.Equal("LOGON ACCEPTED", Assert.Single(acarsConnection.SentMessages).Content);

        // Assert - a new connection replaces the existing connection, and does not keep its state
        var connection = Assert.Single(await aircraftRepository.All(CancellationToken.None));
        Assert.NotSame(existing, connection);
        Assert.Equal(DataAuthorityState.NextDataAuthority, connection.DataAuthorityState);
        Assert.False(connection.HasNextDataAuthority);

        // Assert - the new connection is established, so that a new ADS-C contract is requested
        await mediator.Received(1).Publish(
            new AircraftConnectionEstablished("hoppies-ybbb", "YBBB", "QFA1", DataAuthorityState.NextDataAuthority),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingConnection_PublishesReset()
    {
        // Arrange
        var clock = new TestClock();
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var mediator = Substitute.For<IMediator>();

        await AddController(controllerRepository);
        await AddExistingConnection(aircraftRepository, clock);

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(acarsConnectionManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(
            new AircraftConnectionReset("hoppies-ybbb", "YBBB", "QFA1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingConnection_ClosesOpenDialoguesWithResetMessage()
    {
        // Arrange
        var clock = new TestClock();
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        messageIdProvider.SetNextId(10);
        var mediator = Substitute.For<IMediator>();

        await AddController(controllerRepository);
        await AddExistingConnection(aircraftRepository, clock);

        var openDialogue = new Dialogue("QFA1", "YBBB");
        openDialogue.AddUplink(5, null, "QFA1", "BN-TSN_FSS", CpdlcUplinkResponseType.WilcoUnable, AlertType.None, "CLIMB TO FL410", clock.UtcNow());
        await dialogueRepository.Add(openDialogue, CancellationToken.None);

        // A dialogue with a different ATSU is not part of the connection
        var otherStationDialogue = new Dialogue("QFA1", "YMMM");
        otherStationDialogue.AddUplink(6, null, "QFA1", "ML-IND_FSS", CpdlcUplinkResponseType.WilcoUnable, AlertType.None, "CLIMB TO FL390", clock.UtcNow());
        await dialogueRepository.Add(otherStationDialogue, CancellationToken.None);

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(acarsConnectionManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - the open dialogue is closed with the reset message
        Assert.True(openDialogue.IsClosed);
        var resetMessage = Assert.IsType<DownlinkMessage>(openDialogue.Messages.Last());
        Assert.Equal("CPDLC CONNECTION RESET", resetMessage.Content);
        Assert.Equal(10, resetMessage.MessageId);
        Assert.Equal(5, resetMessage.MessageReference);

        // Assert - the dialogue with the other ATSU does not change
        Assert.False(otherStationDialogue.IsClosed);
        Assert.Single(otherStationDialogue.Messages);
    }

    [Fact]
    public async Task Handle_NotifiesATC()
    {
        // Arrange
        var clock = new TestClock();
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var aircraftRepository = new TestAircraftRepository();
        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var messageIdProvider = new TestMessageIdProvider();
        var mediator = Substitute.For<IMediator>();

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "Connection-1",
            "YBBB",
            "BN-TSN_FSS");
        await controllerRepository.Add(controller, CancellationToken.None);

        var command = BuildLogonCommand(clock);
        var handler = BuildHandler(acarsConnectionManager, aircraftRepository, controllerRepository, dialogueRepository, messageIdProvider, clock, mediator);

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
