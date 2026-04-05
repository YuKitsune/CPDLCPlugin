using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record ArchiveRequest(Guid DialogueId) : IRequest;

public class ArchiveRequestHandler(
    Plugin plugin,
    ILogger logger)
    : IRequestHandler<ArchiveRequest>
{
    public async Task Handle(ArchiveRequest request, CancellationToken cancellationToken)
    {
        logger.Verbose("Archiving dialogue {DialogueId}", request.DialogueId);
        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning("Not connected to server");
            return;
        }

        await plugin.ConnectionManager.ArchiveDialogue(request.DialogueId, cancellationToken);
        logger.Information("Archived dialogue {DialogueId}", request.DialogueId);
    }
}
