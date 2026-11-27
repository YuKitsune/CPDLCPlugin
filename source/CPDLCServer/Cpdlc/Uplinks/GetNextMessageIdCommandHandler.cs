using MediatR;

namespace CPDLCServer.Cpdlc.Uplinks;

public class GetNextMessageIdCommandHandler(IMessageIdProvider messageIdProvider)
    : IRequestHandler<GetNextMessageIdCommand, int>
{
    public async Task<int> Handle(GetNextMessageIdCommand request, CancellationToken cancellationToken)
    {
        return await messageIdProvider.GetNextMessageId(request.AcarsConnectionId, request.Callsign, cancellationToken);
    }
}
