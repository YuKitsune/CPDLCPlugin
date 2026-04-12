using System.Threading.Channels;
using CPDLCServer.Exceptions;
using CPDLCServer.Infrastructure;
using CPDLCServer.Model;

namespace CPDLCServer.Clients;

// TODO: ADS-C
// TODO: Accept encoded messages via the API (i.e. UMxxx), translate from within the client.

public class HoppieAcarsClient : IAcarsClient
{
    readonly Dictionary<string, string> _uplinkMessageTranslations = new()
    {
        { "END SERVICE", "LOGOFF" }
    };

    readonly HoppiesConfiguration _configuration;
    readonly HttpClient _httpClient;
    readonly IClock _clock;
    readonly ILogger _logger;

    readonly Channel<DownlinkMessage> _messageChannel = Channel.CreateUnbounded<DownlinkMessage>();
    readonly Random _random = Random.Shared;

    CancellationTokenSource? _pollCancellationTokenSource;
    Task? _pollTask;
    DateTimeOffset? _lastSendTime;

    bool _disposed;

    public ChannelReader<DownlinkMessage> MessageReader => _messageChannel.Reader;
    public string StationId => _configuration.StationIdentifier;

    public HoppieAcarsClient(HoppiesConfiguration configuration, HttpClient httpClient, IClock clock, ILogger logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _clock = clock;
        _logger = logger.ForContext("ClientId", _configuration.ClientId);
    }

    public Task Connect(CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HoppieAcarsClient));

        _pollCancellationTokenSource = new CancellationTokenSource();
        _pollTask = Poll(_pollCancellationTokenSource.Token);

        _logger.Information("Connected to Hoppies ACARS network");

        return Task.CompletedTask;
    }

    public async Task Send(UplinkMessage message, CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HoppieAcarsClient));

        var messageType = "cpdlc";
        var to = message.Recipient;
        var packet = SerializeCpdlcMessage(message);

        var parameters = new Dictionary<string, string>
        {
            ["logon"] = _configuration.AuthenticationCode,
            ["network"] = _configuration.FlightSimulationNetwork,
            ["from"] = _configuration.StationIdentifier,
            ["to"] = to,
            ["type"] = messageType
        };

        if (!string.IsNullOrEmpty(packet))
            parameters["packet"] = packet;

        var content = new FormUrlEncodedContent(parameters);

        try
        {
            _logger.Verbose("Sending uplink to {Recipient} with packet {Packet}", message.Recipient, packet);
            var response = await _httpClient.PostAsync(_configuration.Url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.Verbose("Send response: {Response}", responseText);

            _lastSendTime = _clock.UtcNow();
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "Failed to send message");
            throw;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.Warning("Send request timed out, skipping");
        }
    }

    public async Task<string[]> ListConnections(CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HoppieAcarsClient));

        var parameters = new Dictionary<string, string>
        {
            ["logon"] = _configuration.AuthenticationCode,
            ["from"] = _configuration.StationIdentifier,
            ["to"] = "SERVER",
            ["type"] = "ping",
            ["packet"] = "ALL-CALLSIGNS"
        };

        var content = new FormUrlEncodedContent(parameters);

        try
        {
            _logger.Verbose("Listing connections");
            var response = await _httpClient.PostAsync(_configuration.Url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.Verbose("ListConnections response: {Response}", responseText);

            // Parse the response to extract callsigns
            if (string.IsNullOrWhiteSpace(responseText))
                return Array.Empty<string>();

            // Strip "ok" prefix if present
            if (responseText.StartsWith("ok", StringComparison.OrdinalIgnoreCase))
            {
                responseText = responseText[2..].TrimStart();
            }

            if (string.IsNullOrWhiteSpace(responseText))
                return Array.Empty<string>();

            // Strip curly braces if present
            if (responseText.StartsWith('{') && responseText.EndsWith('}'))
            {
                responseText = responseText[1..^1].Trim();
            }

            if (string.IsNullOrWhiteSpace(responseText))
                return Array.Empty<string>();

            // The response contains space-separated callsigns
            var callsigns = responseText
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            _logger.Information("Found {Count} active connections", callsigns.Length);
            return callsigns;
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "Failed to list connections");
            throw;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.Warning("ListConnections request timed out");
            return Array.Empty<string>();
        }
    }

    public async Task Disconnect(CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HoppieAcarsClient));

        if (_pollCancellationTokenSource is not null)
            await _pollCancellationTokenSource.CancelAsync();

        if (_pollTask is not null)
            await _pollTask;

        _logger.Information("Disconnected from Hoppies ACARS network");
    }

    async Task Poll(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.Verbose("Polling for messages");

                    var parameters = new Dictionary<string, string>
                    {
                        ["logon"] = _configuration.AuthenticationCode,
                        ["from"] = _configuration.StationIdentifier,
                        ["to"] = "SERVER",
                        ["type"] = "poll"
                    };

                    var content = new FormUrlEncodedContent(parameters);
                    var response = await _httpClient.PostAsync(_configuration.Url, content, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                    _logger.Verbose("Poll response: {Response}", responseText);

                    if (!string.IsNullOrWhiteSpace(responseText) && responseText != "ok")
                    {
                        ParseAndPublishDownlinkMessages(responseText);
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.Error(ex, "Poll request failed");
                }
                catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
                {
                    _logger.Warning("Poll request timed out, skipping this attempt");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unhandled exception when polling messages");
                }

                var pollInterval = GetPollInterval();
                _logger.Verbose("Poll completed, waiting {PollInterval}", pollInterval);
                await Task.Delay(pollInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.Information("Stopping poll task");
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "Unhandled exception when polling messages");
        }
    }

    void ParseAndPublishDownlinkMessages(string responseText)
    {
        try
        {
            if (responseText.StartsWith("ok", StringComparison.OrdinalIgnoreCase))
            {
                responseText = responseText[2..].TrimStart();
            }

            if (string.IsNullOrWhiteSpace(responseText))
                return;

            if (responseText.StartsWith("error", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Warning("Hoppie server error: {Error}", responseText);
                return;
            }

            var messages = ExtractMessages(responseText);

            _logger.Verbose("Extracted {Count} messages", messages.Count);

            foreach (var messageText in messages)
            {
                _logger.Verbose("Extracted: {Message}", messageText);
                var (from, type, packet) = ParseMessage(messageText);

                var message = type.ToLowerInvariant() switch
                {
                    "cpdlc" => ParseCpdlcDownlink(from, packet),
                    _ => null
                };

                if (message is not null)
                {
                    _logger.Information("Received {MessageType} from {From}", type, from);
                    _messageChannel.Writer.TryWrite(message);
                }
                else
                {
                    _logger.Information("Unsupported message type: {Type}", type);
                }
            }
        }
        catch (Exception ex)
        {
            throw new MessageParseException("Failed to parse downlink messages", ex);
        }
    }

    static List<string> ExtractMessages(string responseText)
    {
        var messages = new List<string>();
        var depth = 0;
        var currentMessage = new System.Text.StringBuilder();

        for (var i = 0; i < responseText.Length; i++)
        {
            var c = responseText[i];

            if (c == '{')
            {
                depth++;
                if (depth == 1)
                    continue;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    messages.Add(currentMessage.ToString());
                    currentMessage.Clear();
                    continue;
                }
            }

            if (depth > 0)
            {
                currentMessage.Append(c);
            }
        }

        return messages;
    }

    static (string from, string type, string packet) ParseMessage(string messageText)
    {
        var firstBraceIndex = messageText.IndexOf('{');
        if (firstBraceIndex == -1)
        {
            var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                throw new Exception($"Invalid message format: {messageText}");

            return (parts[0], parts[1], parts.Length > 2 ? string.Join(" ", parts[2..]) : string.Empty);
        }

        var headerParts = messageText[..firstBraceIndex].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (headerParts.Length < 2)
            throw new Exception($"Invalid message format: {messageText}");

        var packet = messageText[(firstBraceIndex + 1)..];
        if (packet.EndsWith('}'))
            packet = packet[..^1];

        return (headerParts[0], headerParts[1], packet);
    }

    DownlinkMessage ParseCpdlcDownlink(string from, string packet)
    {
        var parts = packet.Split('/', 6);
        if (parts.Length != 6)
            throw new MessageParseException($"Invalid CPDLC packet: Expected 6 components, got {parts.Length}: \"{packet}\"");

        var messageId = !string.IsNullOrEmpty(parts[2]) ? int.Parse(parts[2]) : -1;
        int? replyToId = !string.IsNullOrEmpty(parts[3]) ? int.Parse(parts[3]) : null;
        var responseType = parts[4] switch
        {
            "N" => CpdlcDownlinkResponseType.NoResponse,
            "Y" => CpdlcDownlinkResponseType.ResponseRequired,
            _ => throw new NotSupportedException($"Unsupported CPDLC downlink response type: {parts[4]}")
        };

        var content = parts[5];

        return new DownlinkMessage(
            messageId,
            replyToId,
            from,
            responseType,
            AlertType.None,
            content,
            _clock.UtcNow());
    }

    TimeSpan GetPollInterval()
    {
        if (_lastSendTime.HasValue)
        {
            var timeSinceLastSend = _clock.UtcNow() - _lastSendTime.Value;
            if (timeSinceLastSend < TimeSpan.FromMinutes(1))
            {
                return TimeSpan.FromSeconds(20);
            }
        }

        return TimeSpan.FromSeconds(_random.Next(45, 76));
    }

    string SerializeCpdlcMessage(UplinkMessage uplinkMessage)
    {
        var responseType = uplinkMessage.ResponseType switch
        {
            CpdlcUplinkResponseType.NoResponse => "NE",
            CpdlcUplinkResponseType.WilcoUnable => "WU",
            CpdlcUplinkResponseType.AffirmativeNegative => "AN",
            CpdlcUplinkResponseType.Roger => "R",
            _ => throw new ArgumentException($"Unexpected CpdlcResponseType: {uplinkMessage.ResponseType}")
        };

        var replyToId = uplinkMessage.MessageReference is not null
            ? uplinkMessage.MessageReference.ToString()
            : string.Empty;

        var content = GetTranslatedContent(uplinkMessage);

        return $"/data2/{uplinkMessage.MessageId}/{replyToId}/{responseType}/{content}";
    }

    string GetTranslatedContent(UplinkMessage uplinkMessage)
    {
        var translatedContent = uplinkMessage.Content;
        foreach (var uplinkMessageTranslation in _uplinkMessageTranslations)
        {
            translatedContent = translatedContent.Replace(uplinkMessageTranslation.Key, uplinkMessageTranslation.Value);
        }

        return translatedContent;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await Disconnect(CancellationToken.None);

        if (_pollTask is not null)
            _pollTask.Dispose();

        if (_pollCancellationTokenSource is not null)
            _pollCancellationTokenSource.Dispose();

        _httpClient.Dispose();

        _disposed = true;
    }
}
