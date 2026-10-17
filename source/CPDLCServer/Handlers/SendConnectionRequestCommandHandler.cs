using CPDLCServer.Clients;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Services;
using MediatR;

namespace CPDLCServer.Handlers;

public class SendConnectionRequestCommandHandler(
    IClientManager clientManager,
    IMessageIdProvider messageIdProvider,
    IClock clock,
    ILogger logger)
    : IRequestHandler<SendConnectionRequestCommand>
{
    public async Task Handle(SendConnectionRequestCommand request, CancellationToken cancellationToken)
    {
        var acarsClientId = await clientManager.FindAcarsClientId(request.StationId, cancellationToken);
        if (acarsClientId is null)
            throw new Exception($"No ACARS client is configured for {request.StationId}");

        var client = await clientManager.GetAcarsClient(acarsClientId, cancellationToken);

        var messageId = await messageIdProvider.GetNextMessageId(
            acarsClientId,
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

        await client.Send(uplinkMessage, cancellationToken);
        logger.Information("Sent connection request for {StationId} to {Callsign}", request.StationId, request.Callsign);
    }
}
