using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class SendUplinkCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsMessageToAcarsClient()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var publisher = new TestPublisher();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        // Create aircraft connection
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new SendUplinkCommandHandler(
            aircraftRepository,
            clientManager,
            messageIdProvider,
            dialogueRepository,
            publisher,
            clock,
            Logger.None);

        var command = new SendUplinkCommand(
            "BN-TSN_FSS",
            "UAL123",
            null,
            CpdlcUplinkResponseType.WilcoUnable,
            "CLIMB TO @FL410@");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UplinkMessage.MessageId);

        var client = await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        var testClient = (TestAcarsClient)client;
        Assert.Single(testClient.SentMessages);

        var sentMessage = Assert.IsType<UplinkMessage>(testClient.SentMessages[0]);
        Assert.Equal(1, sentMessage.MessageId);
        Assert.Equal("UAL123", sentMessage.Recipient);
        Assert.Null(sentMessage.MessageReference);
        Assert.Equal(CpdlcUplinkResponseType.WilcoUnable, sentMessage.ResponseType);
        Assert.Equal("CLIMB TO @FL410@", sentMessage.Content);
    }

    [Fact]
    public async Task Handle_SendsReplyToAcarsClient()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var publisher = new TestPublisher();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        // Create aircraft connection
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new SendUplinkCommandHandler(
            aircraftRepository,
            clientManager,
            messageIdProvider,
            dialogueRepository,
            publisher,
            clock,
            Logger.None);

        var command = new SendUplinkCommand(
            "BN-TSN_FSS",
            "UAL123",
            5,
            CpdlcUplinkResponseType.NoResponse,
            "ROGER");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UplinkMessage.MessageId);

        var client = await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        var testClient = (TestAcarsClient)client;
        Assert.Single(testClient.SentMessages);

        var sentMessage = Assert.IsType<UplinkMessage>(testClient.SentMessages[0]);
        Assert.Equal(1, sentMessage.MessageId);
        Assert.Equal("UAL123", sentMessage.Recipient);
        Assert.Equal(5, sentMessage.MessageReference);
        Assert.Equal(CpdlcUplinkResponseType.NoResponse, sentMessage.ResponseType);
        Assert.Equal("ROGER", sentMessage.Content);
    }

    [Fact]
    public async Task Handle_CreatesNewDialogue_ForUplinkWithNoReference()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var publisher = new TestPublisher();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        // Create aircraft connection
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new SendUplinkCommandHandler(
            aircraftRepository,
            clientManager,
            messageIdProvider,
            dialogueRepository,
            publisher,
            clock,
            Logger.None);

        var command = new SendUplinkCommand(
            "BN-TSN_FSS",
            "UAL123",
            null,
            CpdlcUplinkResponseType.WilcoUnable,
            "CLIMB TO FL410");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var dialogue = await dialogueRepository.FindDialogueForMessage(
            "UAL123",
            result.UplinkMessage.MessageId,
            CancellationToken.None);

        Assert.NotNull(dialogue);
        Assert.Single(dialogue.Messages);
        Assert.Equal(result.UplinkMessage, dialogue.Messages[0]);
    }

    [Fact]
    public async Task Handle_AppendsToExistingDialogue_ForUplinkWithReference()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        // Create aircraft connection
        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        // Create existing dialogue with a downlink
        var downlink = new DownlinkMessage(
            5,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            clock.UtcNow());

        var existingDialogue = new Dialogue("UAL123", downlink);
        await dialogueRepository.Add(existingDialogue, CancellationToken.None);

        var publisher = new TestPublisher();
        var handler = new SendUplinkCommandHandler(
            aircraftRepository,
            clientManager,
            messageIdProvider,
            dialogueRepository,
            publisher,
            clock,
            Logger.None);

        var command = new SendUplinkCommand(
            "BN-TSN_FSS",
            "UAL123",
            5,
            CpdlcUplinkResponseType.NoResponse,
            "UNABLE");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var dialogue = await dialogueRepository.FindDialogueForMessage(
            "UAL123",
            5,
            CancellationToken.None);

        Assert.NotNull(dialogue);
        Assert.Equal(2, dialogue.Messages.Count);
        Assert.Contains(result.UplinkMessage, dialogue.Messages);
    }

    [Fact]
    public async Task Handle_RoutesToCorrectAcarsClient()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var publisher = new TestPublisher();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        // Create aircraft connection
        var aircraft1 = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft1.RequestLogon(clock.UtcNow());
        aircraft1.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft1.Callsign, aircraft1.AcarsClientId), aircraft1, CancellationToken.None);

        var aircraft2 = new AircraftConnection("UAL456", "hoppies-ymmm", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft2.RequestLogon(clock.UtcNow());
        aircraft2.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft2.Callsign, aircraft2.AcarsClientId), aircraft2, CancellationToken.None);

        var handler = new SendUplinkCommandHandler(
            aircraftRepository,
            clientManager,
            messageIdProvider,
            dialogueRepository,
            publisher,
            clock,
            Logger.None);

        var command = new SendUplinkCommand(
            "BN-TSN_FSS",
            "UAL456",
            5,
            CpdlcUplinkResponseType.NoResponse,
            "ROGER");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UplinkMessage.MessageId);

        // No messages should've been sent through YBBB
        var ybbbClient = (TestAcarsClient) await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        Assert.Empty(ybbbClient.SentMessages);

        // Aircraft is connected to YMMM, so the YMMM client should send the message
        var ymmmClient = (TestAcarsClient) await clientManager.GetAcarsClient("hoppies-ymmm", CancellationToken.None);
        Assert.Single(ymmmClient.SentMessages);

        var sentMessage = Assert.IsType<UplinkMessage>(ymmmClient.SentMessages[0]);
        Assert.Equal(1, sentMessage.MessageId);
        Assert.Equal("UAL456", sentMessage.Recipient);
        Assert.Equal(5, sentMessage.MessageReference);
        Assert.Equal(CpdlcUplinkResponseType.NoResponse, sentMessage.ResponseType);
        Assert.Equal("ROGER", sentMessage.Content);
    }

    [Fact]
    public async Task Handle_WithMultipleConnections_PrefersCurrentDataAuthority()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var dialogueRepository = new TestDialogueRepository();
        var publisher = new TestPublisher();
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();

        // Create aircraft connection
        var aircraft1 = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft1.RequestLogon(clock.UtcNow());
        aircraft1.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft1.Callsign, aircraft1.AcarsClientId), aircraft1, CancellationToken.None);

        var aircraft2 = new AircraftConnection("UAL123", "hoppies-ymmm", "YMMM", DataAuthorityState.NextDataAuthority);
        aircraft2.RequestLogon(clock.UtcNow());
        aircraft2.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft2.Callsign, aircraft2.AcarsClientId), aircraft2, CancellationToken.None);

        var handler = new SendUplinkCommandHandler(
            aircraftRepository,
            clientManager,
            messageIdProvider,
            dialogueRepository,
            publisher,
            clock,
            Logger.None);

        var command = new SendUplinkCommand(
            "BN-TSN_FSS",
            "UAL123",
            null,
            CpdlcUplinkResponseType.WilcoUnable,
            "CLIMB TO @FL410@");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UplinkMessage.MessageId);

        var client = await clientManager.GetAcarsClient("hoppies-ybbb", CancellationToken.None);
        var testClient = (TestAcarsClient)client;
        Assert.Single(testClient.SentMessages);

        var sentMessage = Assert.IsType<UplinkMessage>(testClient.SentMessages[0]);
        Assert.Equal(1, sentMessage.MessageId);
        Assert.Equal("UAL123", sentMessage.Recipient);
        Assert.Null(sentMessage.MessageReference);
        Assert.Equal(CpdlcUplinkResponseType.WilcoUnable, sentMessage.ResponseType);
        Assert.Equal("CLIMB TO @FL410@", sentMessage.Content);
    }
}
