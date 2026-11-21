using CPDLCServer.AcarsConnections;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public class SendConnectionRequestCommandHandler(
    IAcarsConnectionManager acarsConnectionManager,
    IMessageIdProvider messageIdProvider,
    IClock clock,
    IUnitOfWork unitOfWork,
    ILogger logger)
    : IRequestHandler<SendConnectionRequestCommand>
{
    public async Task Handle(SendConnectionRequestCommand request, CancellationToken cancellationToken)
    {
        var acarsConnectionId = await acarsConnectionManager.FindAcarsConnectionId(request.StationId, cancellationToken);
        if (acarsConnectionId is null)
            throw new Exception($"No ACARS connection is configured for {request.StationId}");

        var acarsConnection = await acarsConnectionManager.GetAcarsConnection(acarsConnectionId, cancellationToken);

        var messageId = await messageIdProvider.GetNextMessageId(
            acarsConnectionId,
            request.Callsign,
            cancellationToken);

        // This is a system message. It does not start a dialogue.
        var uplinkMessage = new UplinkMessage(
            Guid.Empty,
            messageId,
            null,
            request.Callsign,
            "SYSTEM",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            $"CONNECTION REQUESTED @{request.StationId}@",
            clock.UtcNow());

        unitOfWork.AfterCommit(async ct =>
        {
            await acarsConnection.Send(uplinkMessage, ct);
            logger.Information("Sent connection request for {StationId} to {Callsign}", request.StationId, request.Callsign);
        });
    }
}
