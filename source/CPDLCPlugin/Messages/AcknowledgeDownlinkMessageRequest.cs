using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record AcknowledgeDownlinkMessageRequest(Guid DialogueId, int MessageId) : IRequest;

public class AcknowledgeDownlinkMessageRequestHandler(Plugin plugin, ILogger logger)
    : IRequestHandler<AcknowledgeDownlinkMessageRequest>
{
    public async Task Handle(AcknowledgeDownlinkMessageRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Acknowledging downlink message {MessageId} from dialogue {Dialogue}", request.MessageId, request.DialogueId);
        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning("Not connected to server");
            return;
        }

        await plugin.ConnectionManager.AcknowledgeDownlink(request.DialogueId, request.MessageId, cancellationToken);

        logger.Verbose("Acknowledged downlink with id {MessageId} in dialogue {DialogueId}",
            request.MessageId,
            request.DialogueId);
    }
}
