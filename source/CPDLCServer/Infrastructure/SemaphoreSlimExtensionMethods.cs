namespace CPDLCServer.Infrastructure;

public static class SemaphoreSlimExtensionMethods
{
    public static async Task<IDisposable> LockAsync(
        this SemaphoreSlim semaphoreSlim,
        CancellationToken cancellationToken)
    {
        await semaphoreSlim.WaitAsync(cancellationToken);
        return new Releaser(semaphoreSlim);
    }

    readonly struct Releaser(SemaphoreSlim semaphoreSlim) : IDisposable
    {
        public void Dispose() => semaphoreSlim.Release();
    }
}