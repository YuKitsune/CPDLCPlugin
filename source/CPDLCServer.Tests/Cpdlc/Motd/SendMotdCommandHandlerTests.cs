using CPDLC.Messages;
using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Cpdlc.Motd;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Cpdlc.Motd;

public class SendMotdCommandHandlerTests
{
    readonly TestAircraftRepository _aircraftRepository = new();
    readonly TestAcarsConnectionManager _acarsConnectionManager = new();
    readonly IMediator _mediator = Substitute.For<IMediator>();
    readonly TestClock _clock = new();

    SendMotdCommandHandler CreateHandler() => new(_aircraftRepository, _acarsConnectionManager, _mediator, Logger.None);

    async Task AddAircraft(string callsign, DataAuthorityState dataAuthorityState)
    {
        var aircraft = new AircraftConnection(callsign, "hoppies-ybbb", "YBBB", dataAuthorityState);
        aircraft.RequestLogon(_clock.UtcNow());
        aircraft.AcceptLogon(_clock.UtcNow());
        await _aircraftRepository.Add(new AircraftKey(aircraft.Callsign, aircraft.AcarsConnectionId), aircraft, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_SendsTheMessageAsFreeTextWithNoResponse()
    {
        // Arrange
        _acarsConnectionManager.AddConfiguration(TestAcarsConnectionManager.CreateConfiguration("YBBB", 10, motd: "WELCOME TO BRISBANE"));
        await AddAircraft("UAL123", DataAuthorityState.CurrentDataAuthority);

        // Act
        await CreateHandler().Handle(new SendMotdCommand("hoppies-ybbb", "UAL123"), CancellationToken.None);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<BeginDialogueCommand>(c =>
                c.Sender == "YBBB" &&
                c.Recipient == "UAL123" &&
                c.Elements.Single().Id == 169 &&
                c.Elements.Single().Parameters.Single() == "WELCOME TO BRISBANE" &&
                !c.CanAction),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAircraftDisconnected_DoesNotSend()
    {
        // Arrange
        _acarsConnectionManager.AddConfiguration(TestAcarsConnectionManager.CreateConfiguration("YBBB", 10, motd: "WELCOME TO BRISBANE"));

        // Act
        await CreateHandler().Handle(new SendMotdCommand("hoppies-ybbb", "UAL123"), CancellationToken.None);

        // Assert
        await _mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<BeginDialogueCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNextDataAuthority_DoesNotSend()
    {
        // Arrange
        _acarsConnectionManager.AddConfiguration(TestAcarsConnectionManager.CreateConfiguration("YBBB", 10, motd: "WELCOME TO BRISBANE"));
        await AddAircraft("UAL123", DataAuthorityState.NextDataAuthority);

        // Act
        await CreateHandler().Handle(new SendMotdCommand("hoppies-ybbb", "UAL123"), CancellationToken.None);

        // Assert
        await _mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<BeginDialogueCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTheMessageWasRemoved_DoesNotSend()
    {
        // Arrange
        await AddAircraft("UAL123", DataAuthorityState.CurrentDataAuthority);

        // Act
        await CreateHandler().Handle(new SendMotdCommand("hoppies-ybbb", "UAL123"), CancellationToken.None);

        // Assert
        await _mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<BeginDialogueCommand>(), Arg.Any<CancellationToken>());
    }
}
