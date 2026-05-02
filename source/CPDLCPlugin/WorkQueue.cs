using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Serilog;

namespace CPDLCPlugin;

public class WorkQueue : IAsyncDisposable
{
    readonly Action<Exception> _onError;
    readonly ILogger _logger;
    readonly IClock _clock;
    readonly Channel<Func<Task>> _workQueue = Channel.CreateUnbounded<Func<Task>>();

    readonly CancellationTokenSource _cancellationTokenSource;
    readonly Task _worker;

    static readonly TimeSpan WaitWarningThreshold = TimeSpan.FromSeconds(2);
    static readonly TimeSpan ExecutionWarningThreshold = TimeSpan.FromSeconds(5);

    public WorkQueue(Action<Exception> onError, ILogger logger, IClock clock)
    {
        _onError = onError;
        _logger = logger;
        _clock = clock;

        _cancellationTokenSource = new CancellationTokenSource();
        _worker = Worker(_cancellationTokenSource.Token);
    }

    public bool Enqueue(Func<Task> work, [CallerMemberName] string caller = "")
    {
        var enqueueTime = _clock.UtcNow();
        return _workQueue.Writer.TryWrite(async () =>
        {
            var waitTime = _clock.UtcNow() - enqueueTime;
            if (waitTime > WaitWarningThreshold)
                _logger.Warning("[{Caller}] Work item waited {WaitMs}ms in queue", caller, (long)waitTime.TotalMilliseconds);

            var sw = Stopwatch.StartNew();
            await work();
            sw.Stop();

            if (sw.Elapsed > ExecutionWarningThreshold)
                _logger.Warning("[{Caller}] Work item took {ElapsedMs}ms to complete", caller, sw.ElapsedMilliseconds);
        });
    }

    async Task Worker(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var work = await _workQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                await work().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                try
                {
                    _onError(ex);
                }
                catch
                {
                    // Ignore errors during error reporting
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cancellationTokenSource.Cancel();
        await _worker.ConfigureAwait(false);

        _worker.Dispose();
        _cancellationTokenSource.Dispose();
    }
}
