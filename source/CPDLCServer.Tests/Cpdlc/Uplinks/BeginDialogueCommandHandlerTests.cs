using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Cpdlc.Uplinks;

public class BeginDialogueCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesNewDialogue()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsConnectionId), aircraft, CancellationToken.None);

        var handler = new BeginDialogueCommandHandler(
            aircraftRepository, acarsConnectionManager, messageIdProvider, dialogueRepository, mediator, clock, new TestUnitOfWork(), Logger.None);

        var command = new BeginDialogueCommand("BN-TSN_FSS", "UAL123", CpdlcUplinkResponseType.WilcoUnable, "CLIMB TO FL410");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var dialogues = await dialogueRepository.All(CancellationToken.None);
        Assert.Single(dialogues);
        Assert.Equal("UAL123", dialogues[0].AircraftCallsign);
        Assert.Single(dialogues[0].Messages);
        Assert.IsType<UplinkMessage>(dialogues[0].Messages[0]);
    }

    [Fact]
    public async Task Handle_UplinkHasNullMessageReference()
    {
        // A dialogue begun by the controller is never a reply - MessageReference must be null.
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsConnectionId), aircraft, CancellationToken.None);

        var handler = new BeginDialogueCommandHandler(
            aircraftRepository, acarsConnectionManager, messageIdProvider, dialogueRepository, mediator, clock, new TestUnitOfWork(), Logger.None);

        var command = new BeginDialogueCommand("BN-TSN_FSS", "UAL123", CpdlcUplinkResponseType.WilcoUnable, "CLIMB TO FL410");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var dialogues = await dialogueRepository.All(CancellationToken.None);
        var uplink = Assert.IsType<UplinkMessage>(dialogues[0].Messages[0]);
        Assert.Null(uplink.MessageReference);
    }

    [Fact]
    public async Task Handle_SendsUplinkViaAcarsConnection()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsConnectionId), aircraft, CancellationToken.None);

        var handler = new BeginDialogueCommandHandler(
            aircraftRepository, acarsConnectionManager, messageIdProvider, dialogueRepository, mediator, clock, new TestUnitOfWork(), Logger.None);

        var command = new BeginDialogueCommand("BN-TSN_FSS", "UAL123", CpdlcUplinkResponseType.WilcoUnable, "CLIMB TO FL410");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var acarsConnection = (TestAcarsConnection) await acarsConnectionManager.GetAcarsConnection("hoppies-ybbb", CancellationToken.None);
        Assert.Single(acarsConnection.SentMessages);
        Assert.Equal("UAL123", acarsConnection.SentMessages[0].Recipient);
        Assert.Equal("CLIMB TO FL410", acarsConnection.SentMessages[0].Content);
    }

    [Fact]
    public async Task Handle_PublishesDialogueChangedNotification()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsConnectionId), aircraft, CancellationToken.None);

        var handler = new BeginDialogueCommandHandler(
            aircraftRepository, acarsConnectionManager, messageIdProvider, dialogueRepository, mediator, clock, new TestUnitOfWork(), Logger.None);

        var command = new BeginDialogueCommand("BN-TSN_FSS", "UAL123", CpdlcUplinkResponseType.WilcoUnable, "CLIMB TO FL410");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(Arg.Any<DialogueChangedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ThrowsWhenAircraftNotConnected()
    {
        // Arrange
        var acarsConnectionManager = new TestAcarsConnectionManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        // No aircraft added

        var handler = new BeginDialogueCommandHandler(
            aircraftRepository, acarsConnectionManager, messageIdProvider, dialogueRepository, mediator, clock, new TestUnitOfWork(), Logger.None);

        var command = new BeginDialogueCommand("BN-TSN_FSS", "UAL123", CpdlcUplinkResponseType.WilcoUnable, "CLIMB TO FL410");

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
    }
}
