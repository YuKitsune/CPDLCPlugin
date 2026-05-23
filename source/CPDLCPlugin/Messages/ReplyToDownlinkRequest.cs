using CPDLCServer.Contracts;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record ReplyToDownlinkRequest(
    string Recipient,
    Guid DialogueId,
    int DownlinkMessageId,
    CpdlcUplinkResponseType ResponseType,
    string Content)
    : IRequest;

public class ReplyToDownlinkRequestHandler(Plugin plugin, ILogger logger)
    : IRequestHandler<ReplyToDownlinkRequest>
{
    public async Task Handle(ReplyToDownlinkRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Replying to downlink {DownlinkMessageId} in dialogue {DialogueId} for {Recipient}",
            request.DownlinkMessageId, request.DialogueId, request.Recipient);

        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning("Not connected to server");
            return;
        }

        await plugin.ConnectionManager.ReplyToDownlink(
            request.DialogueId,
            request.DownlinkMessageId,
            request.ResponseType,
            request.Content,
            cancellationToken);
    }
}
