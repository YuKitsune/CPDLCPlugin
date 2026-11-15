using CPDLCServer.AcarsConnections;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Infrastructure;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog.Core;

namespace CPDLCServer.Tests.Cpdlc.Uplinks;

public class UplinkTransmitterTests
{
    static UplinkMessage CreateUplink(Guid dialogueId) => new(
        dialogueId,
        3,
        null,
        "UAL123",
        "BN-TSN_FSS",
        CpdlcUplinkResponseType.WilcoUnable,
        AlertType.None,
        "CLIMB TO FL410",
        DateTimeOffset.UtcNow);

    [Fact]
    public void TransmitAfterCommit_SendSucceeds_DoesNotMarkUplinkAsFailed()
    {
        // Arrange
        var acarsConnection = new TestAcarsConnection("YBBB");
        var unitOfWorkMediator = Substitute.For<IUnitOfWorkMediator>();
        var transmitter = new UplinkTransmitter(new TestUnitOfWork(), unitOfWorkMediator, Logger.None);
        var uplink = CreateUplink(Guid.NewGuid());

        // Act
        transmitter.TransmitAfterCommit(acarsConnection, uplink);

        // Assert
        Assert.Equal([uplink], acarsConnection.SentMessages);
        unitOfWorkMediator.DidNotReceive().Send(Arg.Any<IRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void TransmitAfterCommit_SendFails_MarksUplinkAsFailedThenThrows()
    {
        // Arrange
        var acarsConnection = Substitute.For<IAcarsConnection>();
        acarsConnection.Send(Arg.Any<UplinkMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Hoppie is not available"));

        var unitOfWorkMediator = Substitute.For<IUnitOfWorkMediator>();
        var transmitter = new UplinkTransmitter(new TestUnitOfWork(), unitOfWorkMediator, Logger.None);
        var dialogueId = Guid.NewGuid();

        // Act
        Assert.Throws<HttpRequestException>(() => transmitter.TransmitAfterCommit(acarsConnection, CreateUplink(dialogueId)));

        // Assert
        unitOfWorkMediator.Received(1).Send(
            new MarkUplinkTransmissionFailedCommand(dialogueId, 3),
            Arg.Any<CancellationToken>());
    }
}
