namespace CPDLCServer.Persistence;

/// <summary>
/// One unit of work for each service scope.
/// The changes in all of the repositories are saved together. Then the messages are sent in the order that they were added.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Runs the action after the changes are saved.
    /// Use this for all messages to controllers and aircraft, so that they do not see changes that were not saved.
    /// </summary>
    void AfterCommit(Func<CancellationToken, Task> action);
}

public class UnitOfWork(CpdlcDbContext dbContext) : IUnitOfWork
{
    readonly List<Func<CancellationToken, Task>> _afterCommitActions = [];

    public void AfterCommit(Func<CancellationToken, Task> action)
    {
        _afterCommitActions.Add(action);
    }

    public async Task Commit(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);

        var actions = _afterCommitActions.ToArray();
        _afterCommitActions.Clear();

        // The changes are saved, so a failure of one message must not stop the other messages
        var exceptions = new List<Exception>();
        foreach (var action in actions)
        {
            try
            {
                await action(cancellationToken);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
            throw new AggregateException("The changes were saved, but one or more messages were not sent", exceptions);
    }
}
