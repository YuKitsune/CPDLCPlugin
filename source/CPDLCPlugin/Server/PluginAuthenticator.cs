using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CPDLCServer.Contracts;
using Serilog;

namespace CPDLCPlugin.Server;

/// <summary>
/// Gets a token from the CPDLC server with the browser (OAuth authorization code with PKCE and a loopback redirect).
/// The plugin opens the login page in the browser. After the login, the browser goes to a port on this computer with a
/// one-use code. Then the plugin changes the code for a token.
/// </summary>
public class PluginAuthenticator(ILogger logger)
{
    static readonly TimeSpan LoginTimeout = TimeSpan.FromMinutes(5);
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    readonly HttpClient _httpClient = CreateHttpClient();

    // With the BuiltIn login, the server makes the token from the CID without a form. With the VATSIM login, the server
    // does not use the CID.
    public async Task<PluginTokenResponse> Authenticate(Uri server, string? cid, CancellationToken cancellationToken)
    {
        // A TcpListener does not need the URL reservation that an HttpListener needs
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var redirectUri = $"http://127.0.0.1:{port}/callback";
            var codeVerifier = Base64Url(RandomBytes(32));
            var codeChallenge = Base64Url(Sha256(codeVerifier));
            var state = Base64Url(RandomBytes(16));

            var authorizeUri = new Uri(
                server,
                "plugin/authorize" +
                $"?redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&state={state}" +
                $"&code_challenge={codeChallenge}" +
                "&code_challenge_method=S256" +
                (string.IsNullOrEmpty(cid) ? string.Empty : $"&cid={Uri.EscapeDataString(cid)}"));

            logger.Information("Opening the browser to authenticate with {Server}", server);
            Process.Start(new ProcessStartInfo(authorizeUri.ToString()) { UseShellExecute = true });

            var code = await WaitForCode(listener, state, cancellationToken);

            logger.Information("Getting a token from {Server}", server);
            var response = await _httpClient.PostAsync(
                new Uri(server, "plugin/token"),
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = code,
                    ["code_verifier"] = codeVerifier,
                    ["redirect_uri"] = redirectUri
                }),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"The CPDLC server did not give a token ({(int)response.StatusCode})");

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PluginTokenResponse>(json, JsonOptions)
                   ?? throw new Exception("The CPDLC server sent an empty token");
        }
        finally
        {
            listener.Stop();
        }
    }

    // Returns null when the server does not accept the token
    public async Task<PluginUserDto?> GetUser(Uri server, string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(server, "plugin/me"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return null;

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PluginUserDto>(json, JsonOptions);
    }

    public async Task Logout(Uri server, string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(server, "plugin/logout"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        // The token is not valid any more, so it is already logged out
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return;

        response.EnsureSuccessStatusCode();
    }

    async Task<string> WaitForCode(TcpListener listener, string expectedState, CancellationToken cancellationToken)
    {
        using var timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellationTokenSource.CancelAfter(LoginTimeout);

        // AcceptTcpClientAsync has no cancellation token in .NET Framework. Stopping the listener ends the wait.
        using var _ = timeoutCancellationTokenSource.Token.Register(listener.Stop);

        while (true)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync();
            }
            catch (Exception ex) when ((ex is ObjectDisposedException or SocketException) && timeoutCancellationTokenSource.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException("The authentication was not completed in 5 minutes");
            }

            using (client)
            {
                var stream = client.GetStream();
                var query = await ReadCallbackQuery(stream);

                // For example, the browser asks for /favicon.ico
                if (query is null)
                {
                    await WriteResponse(stream, "404 Not Found", "Not found.");
                    continue;
                }

                query.TryGetValue("state", out var state);
                query.TryGetValue("code", out var code);

                if (state != expectedState || string.IsNullOrEmpty(code))
                {
                    await WriteResponse(stream, "400 Bad Request", "The authentication failed. Try again from the SETUP window in vatSys.");
                    throw new Exception("The CPDLC server sent a response that is not valid");
                }

                await WriteResponse(stream, "200 OK", "The CPDLC plugin is authenticated. You can close this window and go back to vatSys.");
                return code!;
            }
        }
    }

    // Reads the request line, for example "GET /callback?code=...&state=... HTTP/1.1".
    // Returns null when the request is not for the callback.
    static async Task<Dictionary<string, string>?> ReadCallbackQuery(Stream stream)
    {
        var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
        var requestLine = await reader.ReadLineAsync();

        // Read the headers, so that the browser has sent all of the request before the response
        string? header;
        do
        {
            header = await reader.ReadLineAsync();
        }
        while (!string.IsNullOrEmpty(header));

        var parts = requestLine?.Split(' ');
        if (parts is not { Length: 3 } || parts[0] != "GET")
            return null;

        var target = parts[1];
        var queryStart = target.IndexOf('?');
        var path = queryStart < 0 ? target : target.Substring(0, queryStart);
        if (path != "/callback" || queryStart < 0)
            return null;

        var query = new Dictionary<string, string>();
        foreach (var pair in target.Substring(queryStart + 1).Split('&'))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0)
                continue;

            var name = Uri.UnescapeDataString(pair.Substring(0, separator));
            var value = Uri.UnescapeDataString(pair.Substring(separator + 1).Replace('+', ' '));
            query[name] = value;
        }

        return query;
    }

    static async Task WriteResponse(Stream stream, string status, string message)
    {
        var body = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>CPDLC plugin</title></head>" +
                   "<body style=\"font-family: sans-serif; margin: 3rem;\"><p>" + WebUtility.HtmlEncode(message) + "</p></body></html>";
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        var headers = $"HTTP/1.1 {status}\r\n" +
                      "Content-Type: text/html; charset=utf-8\r\n" +
                      $"Content-Length: {bodyBytes.Length}\r\n" +
                      "Connection: close\r\n\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(headers);

        await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
        await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
        await stream.FlushAsync();
    }

    static byte[] RandomBytes(int count)
    {
        var bytes = new byte[count];
        using var random = RandomNumberGenerator.Create();
        random.GetBytes(bytes);
        return bytes;
    }

    static byte[] Sha256(string text)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.ASCII.GetBytes(text));
    }

    static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    static HttpClient CreateHttpClient()
    {
#if DEBUG
        // In DEBUG mode, bypass SSL certificate validation for local development
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
#else
        return new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
#endif
    }
}
