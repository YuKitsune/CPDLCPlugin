using CPDLCServer.Persistence;

namespace CPDLCServer.Cpdlc.Uplinks;

public interface IMessageIdProvider
{
    Task<int> GetNextMessageId(
        string acarsConnectionId,
        string callsign,
        CancellationToken cancellationToken);
}

// The last message ID is saved with the unit of work, so that the IDs continue after a restart
public class MessageIdProvider(CpdlcDbContext dbContext) : IMessageIdProvider
{
    public async Task<int> GetNextMessageId(
        string acarsConnectionId,
        string callsign,
        CancellationToken cancellationToken)
    {
        var sequence = await dbContext.MessageIdSequences.FindAsync([acarsConnectionId, callsign], cancellationToken);
        if (sequence is null)
        {
            sequence = new MessageIdSequence(acarsConnectionId, callsign);
            dbContext.MessageIdSequences.Add(sequence);
        }

        return sequence.Next();
    }
}
