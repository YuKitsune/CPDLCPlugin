using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

// Send this request after the FDR was updated from an uplink, so that the "X" prefix is removed for all controllers
public record MarkUplinkActionedRequest(Guid DialogueId, int UplinkMessageId) : IRequest;

public class MarkUplinkActionedRequestHandler(Plugin plugin, ILogger logger)
    : IRequestHandler<MarkUplinkActionedRequest>
{
    public async Task Handle(MarkUplinkActionedRequest request, CancellationToken cancellationToken)
    {
        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning("Not connected to server");
            return;
        }

        await plugin.ConnectionManager.MarkUplinkActioned(request.DialogueId, request.UplinkMessageId, cancellationToken);

        logger.Information("Marked uplink {MessageId} in dialogue {DialogueId} as actioned",
            request.UplinkMessageId,
            request.DialogueId);
    }
}
