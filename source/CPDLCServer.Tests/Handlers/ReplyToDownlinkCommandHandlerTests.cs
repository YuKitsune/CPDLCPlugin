using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

// Regression tests for issue #34 - "Replies from the CPDLC Editor are starting new messages"
// Root cause: the old implementation searched for dialogues by MessageId, which is only scoped
// per-session and collides across message types. The fix uses DialogueId for lookup so replies
// are always appended to the correct existing dialogue.
public class ReplyToDownlinkCommandHandlerTests
{
    [Fact]
    public async Task Handle_AppendsUplinkToExistingDialogue_NotCreatingNew()
    {
        // Regression for issue #34: controller replying to a pilot downlink must append
        // to the existing dialogue, not start a new one.
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var dialogue = new Dialogue("UAL123");
        dialogue.AddDownlink(
            7,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST DESCENT FL350",
            clock.UtcNow());
        await dialogueRepository.Add(dialogue, CancellationToken.None);

        var handler = new ReplyToDownlinkCommandHandler(
            aircraftRepository, clientManager, messageIdProvider, dialogueRepository, mediator, clock, Logger.None);

        var command = new ReplyToDownlinkCommand("BN-TSN_FSS", dialogue.Id, 7, CpdlcUplinkResponseType.WilcoUnable, "DESCEND FL350");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - only one dialogue exists (no new dialogue was created)
        var allDialogues = await dialogueRepository.All(CancellationToken.None);
        Assert.Single(allDialogues);

        // Assert - the uplink was added to the existing dialogue
        Assert.Equal(2, allDialogues[0].Messages.Count);
        Assert.IsType<DownlinkMessage>(allDialogues[0].Messages[0]);
        Assert.IsType<UplinkMessage>(allDialogues[0].Messages[1]);
    }

    [Fact]
    public async Task Handle_UplinkMessageReferenceMatchesDownlinkId()
    {
        // The outgoing uplink must carry the downlink's MessageId as its MessageReference
        // so the aircraft knows which of its messages is being replied to.
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var dialogue = new Dialogue("UAL123");
        dialogue.AddDownlink(7, null, "UAL123", CpdlcDownlinkResponseType.ResponseRequired, AlertType.None, "REQUEST DESCENT FL350", clock.UtcNow());
        await dialogueRepository.Add(dialogue, CancellationToken.None);

        var handler = new ReplyToDownlinkCommandHandler(
            aircraftRepository, clientManager, messageIdProvider, dialogueRepository, mediator, clock, Logger.None);

        // Act
        await handler.Handle(
            new ReplyToDownlinkCommand("BN-TSN_FSS", dialogue.Id, 7, CpdlcUplinkResponseType.WilcoUnable, "DESCEND FL350"),
            CancellationToken.None);

        // Assert
        var uplink = dialogue.Messages.OfType<UplinkMessage>().Single();
        Assert.Equal(7, uplink.MessageReference);
    }

    [Fact]
    public async Task Handle_UsesDialogueId_WhenMultipleDialoguesHaveSameDownlinkMessageId()
    {
        // Regression for issue #34: the old code searched by MessageId, which is not globally
        // unique. Two dialogues for the same aircraft can legitimately have messages with the
        // same MessageId. The fix uses DialogueId for unambiguous lookup.
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        // Both dialogues have a downlink with MessageId=1 (aircraft re-used the same ID)
        var dialogueA = new Dialogue("UAL123");
        dialogueA.AddDownlink(1, null, "UAL123", CpdlcDownlinkResponseType.ResponseRequired, AlertType.None, "REQUEST CLIMB FL350", clock.UtcNow());
        await dialogueRepository.Add(dialogueA, CancellationToken.None);

        var dialogueB = new Dialogue("UAL123");
        dialogueB.AddDownlink(1, null, "UAL123", CpdlcDownlinkResponseType.ResponseRequired, AlertType.None, "REQUEST DESCENT FL250", clock.UtcNow());
        await dialogueRepository.Add(dialogueB, CancellationToken.None);

        var handler = new ReplyToDownlinkCommandHandler(
            aircraftRepository, clientManager, messageIdProvider, dialogueRepository, mediator, clock, Logger.None);

        // Controller explicitly replies to dialogueA
        await handler.Handle(
            new ReplyToDownlinkCommand("BN-TSN_FSS", dialogueA.Id, 1, CpdlcUplinkResponseType.WilcoUnable, "CLIMB FL350"),
            CancellationToken.None);

        // Assert - only dialogueA received the uplink
        Assert.Equal(2, dialogueA.Messages.Count);
        Assert.Single(dialogueB.Messages); // dialogueB is untouched
    }

    [Fact]
    public async Task Handle_SendsUplinkViaAcarsClient()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var dialogue = new Dialogue("UAL123");
        dialogue.AddDownlink(7, null, "UAL123", CpdlcDownlinkResponseType.ResponseRequired, AlertType.None, "REQUEST DESCENT FL350", clock.UtcNow());
        await dialogueRepository.Add(dialogue, CancellationToken.None);

        var handler = new ReplyToDownlinkCommandHandler(
            aircraftRepository, clientManager, messageIdProvider, dialogueRepository, mediator, clock, Logger.None);

        // Act
        await handler.Handle(
            new ReplyToDownlinkCommand("BN-TSN_FSS", dialogue.Id, 7, CpdlcUplinkResponseType.WilcoUnable, "DESCEND FL350"),
            CancellationToken.None);

        // Assert
        var client = (TestAcarsClient) await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        Assert.Single(client.SentMessages);
        Assert.Equal("UAL123", client.SentMessages[0].Recipient);
        Assert.Equal("DESCEND FL350", client.SentMessages[0].Content);
        Assert.Equal(7, client.SentMessages[0].MessageReference);
    }

    [Fact]
    public async Task Handle_PublishesDialogueChangedNotification()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var dialogue = new Dialogue("UAL123");
        dialogue.AddDownlink(7, null, "UAL123", CpdlcDownlinkResponseType.ResponseRequired, AlertType.None, "REQUEST DESCENT FL350", clock.UtcNow());
        await dialogueRepository.Add(dialogue, CancellationToken.None);

        var handler = new ReplyToDownlinkCommandHandler(
            aircraftRepository, clientManager, messageIdProvider, dialogueRepository, mediator, clock, Logger.None);

        // Act
        await handler.Handle(
            new ReplyToDownlinkCommand("BN-TSN_FSS", dialogue.Id, 7, CpdlcUplinkResponseType.WilcoUnable, "DESCEND FL350"),
            CancellationToken.None);

        // Assert
        await mediator.Received(1).Publish(
            Arg.Is<DialogueChangedNotification>(n => n.Dialogue.Id == dialogue.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ThrowsWhenDialogueNotFound()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        var handler = new ReplyToDownlinkCommandHandler(
            aircraftRepository, clientManager, messageIdProvider, dialogueRepository, mediator, clock, Logger.None);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            handler.Handle(
                new ReplyToDownlinkCommand("BN-TSN_FSS", Guid.NewGuid(), 1, CpdlcUplinkResponseType.NoResponse, "ROGER"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsWhenAircraftNotConnected()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        // Dialogue exists but aircraft is not in the repository
        var dialogue = new Dialogue("UAL123");
        dialogue.AddDownlink(1, null, "UAL123", CpdlcDownlinkResponseType.ResponseRequired, AlertType.None, "REQUEST DESCENT", clock.UtcNow());
        await dialogueRepository.Add(dialogue, CancellationToken.None);

        var handler = new ReplyToDownlinkCommandHandler(
            aircraftRepository, clientManager, messageIdProvider, dialogueRepository, mediator, clock, Logger.None);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            handler.Handle(
                new ReplyToDownlinkCommand("BN-TSN_FSS", dialogue.Id, 1, CpdlcUplinkResponseType.NoResponse, "ROGER"),
                CancellationToken.None));
    }
}
