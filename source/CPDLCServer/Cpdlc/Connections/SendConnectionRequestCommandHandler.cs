using CPDLCServer.AcarsConnections;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public class SendConnectionRequestCommandHandler(
    IAcarsConnectionManager acarsConnectionManager,
    IMessageIdProvider messageIdProvider,
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

        // This is a control message. It does not start a dialogue.
        unitOfWork.AfterCommit(async ct =>
        {
            await acarsConnection.SendConnectionRequest(request.Callsign, messageId, ct);
            logger.Information("Sent connection request for {StationId} to {Callsign}", request.StationId, request.Callsign);
        });
    }
}
