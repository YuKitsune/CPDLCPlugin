using System.Reflection;
using CPDLCPlugin.Extensions;
using Serilog;
using vatsys;

namespace CPDLCPlugin;

public class AtisCache(IClock clock, IErrorReporter errorReporter, ILogger logger)
{
    readonly SemaphoreSlim _semaphore = new(1, 1);
    readonly Dictionary<string, CacheEntry> _cache = new();
    readonly ILogger _logger = logger;

    readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10);

    record CacheEntry(DateTimeOffset Expiry, string[] Lines);

    public async Task<string[]> GetAtis(string callsign, CancellationToken cancellationToken)
    {
        using (await _semaphore.Lock(cancellationToken))
        {
            var now = clock.UtcNow();
            if (_cache.TryGetValue(callsign, out var cacheEntry))
            {
                if (cacheEntry.Expiry < now)
                {
                    _logger.Verbose("Cache ATIS for {callsign} hit", callsign);
                    return cacheEntry.Lines;
                }

                _cache.Remove(callsign);
                _logger.Information("ATIS for {callsign} is stale", callsign);
            }

            var controller = Network.GetOnlineATCs.FirstOrDefault(c => c.Callsign == callsign);
            if (controller is null)
                return [];

            var requestTimeout = TimeSpan.FromSeconds(10);
            var timeoutCancellationTokenSource = new CancellationTokenSource(requestTimeout);
            var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationTokenSource.Token);

            try
            {
                var atisLines = await WaitForAtis(controller, linkedCancellationTokenSource.Token);
                _cache[callsign] = new CacheEntry(now.Add(_checkInterval), atisLines);
            }
            catch (OperationCanceledException ex) when (linkedCancellationTokenSource.IsCancellationRequested)
            {
                errorReporter.ReportError(ex, $"Timed out fetching ATIS for {callsign}");
            }
            catch (Exception ex)
            {
                errorReporter.ReportError(ex, $"Failed to fetch ATIS for {callsign}");
            }

            return [];
        }
    }

    async Task<string[]> WaitForAtis(NetworkATC controller, CancellationToken cancellationToken)
    {
        var lastUpdatedAtisTime = controller.LastATISUpdate;
        RequestAtis(controller.Callsign);

        _logger.Information("ATIS requested for {callsign}", controller.Callsign);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (lastUpdatedAtisTime == controller.LastATISUpdate)
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            _logger.Verbose("ATIS loaded for {callsign}", controller.Callsign);

            return controller.ATIS ?? [];
        }
    }

    void RequestAtis(string callsign)
    {
        var instanceField = typeof(Network).GetField("Instance", BindingFlags.NonPublic | BindingFlags.Static);
        var getAtisMethod = typeof(Network).GetMethod("RequestATIS", BindingFlags.NonPublic | BindingFlags.Static);

        var networkInstance = instanceField.GetValue(null);
        getAtisMethod.Invoke(networkInstance, new object[] { callsign });
    }
}
