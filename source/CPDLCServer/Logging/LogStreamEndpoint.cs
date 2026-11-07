using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;

namespace CPDLCServer.Logging;

// Sends the log entries to the Logs page as Server-Sent Events.
// First it sends the recent entries, then each new entry until the browser disconnects.
public static class LogStreamEndpoint
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task Handle(HttpContext context, LogBuffer logBuffer, IHostApplicationLifetime lifetime)
    {
        // Stop the stream when the server stops, so that the shutdown does not wait for the browser
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            context.RequestAborted,
            lifetime.ApplicationStopping);
        var cancellationToken = cancellationTokenSource.Token;

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        using var subscription = logBuffer.Subscribe();

        try
        {
            foreach (var entry in subscription.RecentEntries)
                await WriteEvent(context.Response, entry, cancellationToken);

            await context.Response.Body.FlushAsync(cancellationToken);

            await foreach (var entry in subscription.NewEntries.ReadAllAsync(cancellationToken))
            {
                await WriteEvent(context.Response, entry, cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The browser disconnected, or the server is stopping
        }
    }

    static async Task WriteEvent(HttpResponse response, LogEntry entry, CancellationToken cancellationToken)
    {
        // The JSON has no line breaks, so one data line holds the entry
        var json = JsonSerializer.Serialize(entry, JsonOptions);
        await response.WriteAsync($"id: {entry.Sequence}\ndata: {json}\n\n", cancellationToken);
    }
}
