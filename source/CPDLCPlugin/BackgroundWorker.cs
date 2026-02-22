namespace CPDLCPlugin;

public class BackgroundWorker : IAsyncDisposable
{
    readonly Task _workerTask;
    readonly CancellationTokenSource _cancellationTokenSource;

    public BackgroundWorker(Func<CancellationToken, Task> workerTask)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _workerTask = workerTask(_cancellationTokenSource.Token);
    }

    public async ValueTask DisposeAsync()
    {
        _cancellationTokenSource.Cancel();
        try
        {
            await _workerTask;
        }
        catch (OperationCanceledException) when (_cancellationTokenSource.IsCancellationRequested)
        {
            // Ignored, likely thrown due to cancellation
        }

        _workerTask.Dispose();
        _cancellationTokenSource.Dispose();
    }
}
