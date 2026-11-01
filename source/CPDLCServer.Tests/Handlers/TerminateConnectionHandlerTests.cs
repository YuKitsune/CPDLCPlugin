using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class TerminateConnectionHandlerTests
{
    static TerminateConnectionHandler BuildHandler(
        TestAircraftRepository aircraftRepository,
        TestDialogueRepository dialogueRepository,
        TestClock clock,
        IMediator mediator) =>
        new(
            aircraftRepository,
            dialogueRepository,
            clock,
            mediator,
            Logger.None);

    static async Task<AircraftConnection> AddConnectedAircraft(TestAircraftRepository aircraftRepository, TestClock clock, string callsign)
    {
        var aircraft = new AircraftConnection(callsign, "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);
        return aircraft;
    }

    static async Task<Dialogue> AddOpenDialogue(TestDialogueRepository dialogueRepository, TestClock clock, string callsign)
    {
        var dialogue = new Dialogue(callsign);
        dialogue.AddDownlink(
            1,
            null,
            callsign,
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST DESCENT",
            clock.UtcNow());

        await dialogueRepository.Add(dialogue, CancellationToken.None);
        return dialogue;
    }

    [Fact]
    public async Task Handle_RemovesConnectionAndPublishesTerminatedNotification()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        await AddConnectedAircraft(aircraftRepository, clock, "UAL123");

        var handler = BuildHandler(aircraftRepository, dialogueRepository, clock, mediator);

        // Act
        await handler.Handle(new TerminateConnectionRequest("UAL123", "hoppies-ybbb"), CancellationToken.None);

        // Assert
        Assert.Null(await aircraftRepository.Find(new("UAL123", "hoppies-ybbb"), CancellationToken.None));

        await mediator.Received(1).Publish(
            Arg.Is<AircraftConnectionTerminated>(n =>
                n.AcarsClientId == "hoppies-ybbb" &&
                n.StationId == "YBBB" &&
                n.Callsign == "UAL123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ClosesOpenDialoguesForAircraft()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        await AddConnectedAircraft(aircraftRepository, clock, "UAL123");
        var dialogue = await AddOpenDialogue(dialogueRepository, clock, "UAL123");

        var handler = BuildHandler(aircraftRepository, dialogueRepository, clock, mediator);

        // Act
        await handler.Handle(new TerminateConnectionRequest("UAL123", "hoppies-ybbb"), CancellationToken.None);

        // Assert
        Assert.True(dialogue.IsClosed);
        Assert.All(dialogue.Messages, m => Assert.True(m.IsClosed));

        await mediator.Received(1).Publish(
            Arg.Is<DialogueChangedNotification>(n => n.Dialogue.Id == dialogue.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotChangeClosedOrArchivedDialogues()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        await AddConnectedAircraft(aircraftRepository, clock, "UAL123");

        var closedDialogue = new Dialogue("UAL123");
        closedDialogue.AddDownlink(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse,
            AlertType.None,
            "WHEN CAN WE EXPECT HIGHER LEVEL",
            clock.UtcNow());
        await dialogueRepository.Add(closedDialogue, CancellationToken.None);

        var archivedDialogue = await AddOpenDialogue(dialogueRepository, clock, "UAL123");
        archivedDialogue.Archive(clock.UtcNow());

        var handler = BuildHandler(aircraftRepository, dialogueRepository, clock, mediator);

        // Act
        await handler.Handle(new TerminateConnectionRequest("UAL123", "hoppies-ybbb"), CancellationToken.None);

        // Assert
        Assert.False(archivedDialogue.IsClosed);

        await mediator.DidNotReceive().Publish(Arg.Any<DialogueChangedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotChangeDialoguesForOtherAircraft()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        await AddConnectedAircraft(aircraftRepository, clock, "UAL123");
        await AddConnectedAircraft(aircraftRepository, clock, "QFA1");
        var otherDialogue = await AddOpenDialogue(dialogueRepository, clock, "QFA1");

        var handler = BuildHandler(aircraftRepository, dialogueRepository, clock, mediator);

        // Act
        await handler.Handle(new TerminateConnectionRequest("UAL123", "hoppies-ybbb"), CancellationToken.None);

        // Assert
        Assert.False(otherDialogue.IsClosed);

        await mediator.DidNotReceive().Publish(Arg.Any<DialogueChangedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownAircraft_DoesNothing()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var dialogue = await AddOpenDialogue(dialogueRepository, clock, "UAL123");

        var handler = BuildHandler(aircraftRepository, dialogueRepository, clock, mediator);

        // Act
        await handler.Handle(new TerminateConnectionRequest("UAL123", "hoppies-ybbb"), CancellationToken.None);

        // Assert
        Assert.False(dialogue.IsClosed);

        await mediator.DidNotReceive().Publish(Arg.Any<DialogueChangedNotification>(), Arg.Any<CancellationToken>());
        await mediator.DidNotReceive().Publish(Arg.Any<AircraftConnectionTerminated>(), Arg.Any<CancellationToken>());
    }
}
