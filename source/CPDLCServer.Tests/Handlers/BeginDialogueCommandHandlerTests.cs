using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class BeginDialogueCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesNewDialogue()
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

        var handler = new BeginDialogueCommandHandler(
            aircraftRepository, clientManager, messageIdProvider, dialogueRepository, mediator, clock, Logger.None);

        var command = new BeginDialogueCommand("BN-TSN_FSS", "UAL123", CpdlcUplinkResponseType.WilcoUnable, "CLIMB TO FL410");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var dialogues = await dialogueRepository.All(CancellationToken.None);
        Assert.Single(dialogues);
        Assert.Equal("UAL123", dialogues[0].AircraftCallsign);
        Assert.Single(dialogues[0].Messages);
        Assert.Equal(result.UplinkMessage, dialogues[0].Messages[0]);
    }

    [Fact]
    public async Task Handle_UplinkHasNullMessageReference()
    {
        // A dialogue begun by the controller is never a reply - MessageReference must be null.
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

        var handler = new BeginDialogueCommandHandler(
            aircraftRepository, clientManager, messageIdProvider, dialogueRepository, mediator, clock, Logger.None);

        var command = new BeginDialogueCommand("BN-TSN_FSS", "UAL123", CpdlcUplinkResponseType.WilcoUnable, "CLIMB TO FL410");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Null(result.UplinkMessage.MessageReference);
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

        var handler = new BeginDialogueCommandHandler(
            aircraftRepository, clientManager, messageIdProvider, dialogueRepository, mediator, clock, Logger.None);

        var command = new BeginDialogueCommand("BN-TSN_FSS", "UAL123", CpdlcUplinkResponseType.WilcoUnable, "CLIMB TO FL410");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var client = (TestAcarsClient) await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        Assert.Single(client.SentMessages);
        Assert.Equal("UAL123", client.SentMessages[0].Recipient);
        Assert.Equal("CLIMB TO FL410", client.SentMessages[0].Content);
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

        var handler = new BeginDialogueCommandHandler(
            aircraftRepository, clientManager, messageIdProvider, dialogueRepository, mediator, clock, Logger.None);

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
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        // No aircraft added

        var handler = new BeginDialogueCommandHandler(
            aircraftRepository, clientManager, messageIdProvider, dialogueRepository, mediator, clock, Logger.None);

        var command = new BeginDialogueCommand("BN-TSN_FSS", "UAL123", CpdlcUplinkResponseType.WilcoUnable, "CLIMB TO FL410");

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
    }
}
