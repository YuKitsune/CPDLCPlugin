using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record AcknowledgeUplinkMessageRequest(Guid DialogueId, int MessageId) : IRequest;

public class AcknowledgeMessageRequestHandler(Plugin plugin, ILogger logger)
    : IRequestHandler<AcknowledgeUplinkMessageRequest>
{
    public async Task Handle(AcknowledgeUplinkMessageRequest request, CancellationToken cancellationToken)
    {
        logger.Verbose("Manually acknowledging uplink message {MessageId} in dialogue {DialogueId}", request.MessageId, request.DialogueId);
        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning("Not connected to server");
            return;
        }

        await plugin.ConnectionManager.AcknowledgeUplink(request.DialogueId, request.MessageId, cancellationToken);

        logger.Information("Manually acknowledged uplink message {MessageId} in dialogue {DialogueId}",
            request.MessageId,
            request.DialogueId);
    }
}
