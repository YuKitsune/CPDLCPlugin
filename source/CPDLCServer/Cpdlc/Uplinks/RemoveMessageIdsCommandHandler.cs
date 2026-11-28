using MediatR;

namespace CPDLCServer.Cpdlc.Uplinks;

public class RemoveMessageIdsCommandHandler(IMessageIdProvider messageIdProvider)
    : IRequestHandler<RemoveMessageIdsCommand>
{
    public async Task Handle(RemoveMessageIdsCommand request, CancellationToken cancellationToken)
    {
        await messageIdProvider.Remove(request.AcarsConnectionId, request.Callsign, cancellationToken);
    }
}
