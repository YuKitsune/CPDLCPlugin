using CPDLCServer.Persistence;

namespace CPDLCServer.Tests.Mocks;

// Runs each action immediately, so that the handler tests can check the messages after the handler returns
public class TestUnitOfWork : IUnitOfWork
{
    public void AfterCommit(Func<CancellationToken, Task> action)
    {
        action(CancellationToken.None).GetAwaiter().GetResult();
    }
}
