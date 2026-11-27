using CPDLCServer.Infrastructure;

namespace CPDLCServer.Cpdlc.Uplinks;

// A message ID provider for code that is not in a unit of work, for example an ACARS connection.
// MessageIdProvider is scoped, and the IDs are saved when the unit of work is committed. Thus, each call runs in its own
// unit of work.
public class UnitOfWorkMessageIdProvider(IUnitOfWorkMediator mediator) : IMessageIdProvider
{
    public async Task<int> GetNextMessageId(string acarsConnectionId, string callsign, CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetNextMessageIdCommand(acarsConnectionId, callsign), cancellationToken);
    }

    public async Task Remove(string acarsConnectionId, string callsign, CancellationToken cancellationToken)
    {
        await mediator.Send(new RemoveMessageIdsCommand(acarsConnectionId, callsign), cancellationToken);
    }
}
