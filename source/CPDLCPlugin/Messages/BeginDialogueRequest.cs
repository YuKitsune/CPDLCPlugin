using CPDLCServer.Contracts;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record BeginDialogueRequest(
    string Recipient,
    CpdlcUplinkResponseType ResponseType,
    string Content)
    : IRequest;

public class BeginDialogueRequestHandler(Plugin plugin, ILogger logger)
    : IRequestHandler<BeginDialogueRequest>
{
    public async Task Handle(BeginDialogueRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Beginning dialogue with {Recipient}", request.Recipient);

        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning("Not connected to server");
            return;
        }

        await plugin.ConnectionManager.BeginDialogue(
            request.Recipient,
            request.ResponseType,
            request.Content,
            cancellationToken);
    }
}
