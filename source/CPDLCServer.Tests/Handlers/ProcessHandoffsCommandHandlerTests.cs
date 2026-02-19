using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using MediatR;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class ProcessHandoffsCommandHandlerTests
{
    static IConfiguration DefaultConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Handoff:NotificationLeadTime"] = "10" })
            .Build();

    [Fact]
    public async Task Handle_WhenNoNextDataAuthority_DoesNotSendMessage()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var mediator = Substitute.For<IMediator>();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new ProcessHandoffsCommandHandler(
            aircraftRepository,
            mediator,
            clock,
            Logger.None,
            DefaultConfiguration());

        // Act
        await handler.Handle(new ProcessHandoffsCommand(), CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Send(Arg.Any<SendUplinkCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHandoffMessageAlreadySent_DoesNotSendMessage()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var mediator = Substitute.For<IMediator>();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        aircraft.SetNextDataAuthority("YMMM", clock.UtcNow().AddMinutes(5));
        aircraft.SentNextDataAuthorityMessage();
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new ProcessHandoffsCommandHandler(
            aircraftRepository,
            mediator,
            clock,
            Logger.None,
            DefaultConfiguration());

        // Act
        await handler.Handle(new ProcessHandoffsCommand(), CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Send(Arg.Any<SendUplinkCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOutsideNotificationWindow_DoesNotSendMessage()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var mediator = Substitute.For<IMediator>();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());

        // Transfer is 20 minutes away, notification window is 10 minutes, too early to send
        aircraft.SetNextDataAuthority("YMMM", clock.UtcNow().AddMinutes(20));
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new ProcessHandoffsCommandHandler(
            aircraftRepository,
            mediator,
            clock,
            Logger.None,
            DefaultConfiguration());

        // Act
        await handler.Handle(new ProcessHandoffsCommand(), CancellationToken.None);

        // Assert
        await mediator.DidNotReceive().Send(Arg.Any<SendUplinkCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWithinNotificationWindow_SendsNextDataAuthorityMessage()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var mediator = Substitute.For<IMediator>();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());

        // Transfer is 5 minutes away, notification window is 10 minutes, within the window
        aircraft.SetNextDataAuthority("YMMM", clock.UtcNow().AddMinutes(5));
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new ProcessHandoffsCommandHandler(
            aircraftRepository,
            mediator,
            clock,
            Logger.None,
            DefaultConfiguration());

        // Act
        await handler.Handle(new ProcessHandoffsCommand(), CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<SendUplinkCommand>(c =>
                c.Sender == "YBBB" &&
                c.Recipient == "UAL123" &&
                c.Content == "NEXT DATA AUTHORITY @YMMM@" &&
                c.ResponseType == CpdlcUplinkResponseType.NoResponse),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMessageSent_UpdatesAircraftConnection()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var mediator = Substitute.For<IMediator>();

        var aircraft = new AircraftConnection("UAL123", "hoppies-ybbb", "YBBB", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());

        // Transfer is 5 minutes away; notification window is 10 minutes → within window
        aircraft.SetNextDataAuthority("YMMM", clock.UtcNow().AddMinutes(5));
        await aircraftRepository.Add(new(aircraft.Callsign, aircraft.AcarsClientId), aircraft, CancellationToken.None);

        var handler = new ProcessHandoffsCommandHandler(
            aircraftRepository,
            mediator,
            clock,
            Logger.None,
            DefaultConfiguration());

        // Act
        await handler.Handle(new ProcessHandoffsCommand(), CancellationToken.None);

        // Assert
        Assert.True(aircraft.DidSentNextDataAuthorityMessage);
    }
}
