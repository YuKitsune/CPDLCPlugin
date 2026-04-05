using CPDLCServer.Contracts;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record SendUplinkRequest(
    string Recipient,
    int? ReplyToDownlinkId,
    CpdlcUplinkResponseType ResponseType,
    string Content)
    : IRequest;

public class SendUplinkRequestHandler(Plugin plugin, ILogger logger)
    : IRequestHandler<SendUplinkRequest>
{
    public async Task Handle(SendUplinkRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Sending uplink to {Recipient} (ReplyTo: {ReplyToDownlinkId}, Type: {ResponseType}) with content: {Content}",
            request.Recipient, request.ReplyToDownlinkId, request.ResponseType, request.Content);

        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning("Not connected to server");
            return;
        }

        await plugin.ConnectionManager.SendUplink(
            request.Recipient,
            request.ReplyToDownlinkId,
            request.ResponseType,
            request.Content,
            cancellationToken);
    }
}
