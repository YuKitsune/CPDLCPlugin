using System.Net;
using CPDLCServer.Clients;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Clients;

public class HoppieAcarsClientTests : IDisposable
{
    private readonly HoppiesConfiguration _configuration = new()
    {
        Url = new Uri("http://test.hoppie.nl/acars"),
        AuthenticationCode = "TEST123",
        FlightSimulationNetwork = "VATSIM",
        StationIdentifier = "YBBB"
    };

    private readonly List<HoppieAcarsClient> _clients = new();

    public void Dispose()
    {
        foreach (var client in _clients)
        {
            try
            {
                client.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(1));
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is ObjectDisposedException))
            {
                // Already disposed, ignore
            }
        }
    }

    private HoppieAcarsClient CreateClient(TestHttpMessageHandler httpHandler, TestClock? clock = null)
    {
        var httpClient = new HttpClient(httpHandler) { BaseAddress = _configuration.Url };
        var client = new HoppieAcarsClient(
            _configuration,
            httpClient,
            clock ?? new TestClock(),
            Logger.None);
        _clients.Add(client);
        return client;
    }

    [Fact]
    public async Task Send_CpdlcMessage_FormatsPayloadCorrectly()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var clock = new TestClock();
        var client = CreateClient(httpHandler, clock);

        await client.Connect(CancellationToken.None);

        var message = new UplinkMessage(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLIMB TO @FL350@",
            clock.UtcNow());

        // Act
        await client.Send(message, CancellationToken.None);

        // Assert
        var sendRequest = httpHandler.Requests.FirstOrDefault(r =>
        {
            var formDataString = r.Content?.ReadAsStringAsync().Result ?? "";
            return formDataString.Contains("type=cpdlc");
        });

        Assert.NotNull(sendRequest);
        var formData = await ParseFormDataFromRequest(sendRequest);

        Assert.Equal("cpdlc", formData["type"]);
        Assert.Equal("/data2/1//WU/CLIMB+TO+@FL350@", formData["packet"]);
    }

    [Fact]
    public async Task Send_CpdlcReply_IncludesReplyToId()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var clock = new TestClock();
        var client = CreateClient(httpHandler, clock);

        await client.Connect(CancellationToken.None);

        var reply = new UplinkMessage(
            2,
            5,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "ROGER",
            clock.UtcNow());

        // Act
        await client.Send(reply, CancellationToken.None);

        // Assert
        var sendRequest = httpHandler.Requests.FirstOrDefault(r =>
        {
            var formDataString = r.Content?.ReadAsStringAsync().Result ?? "";
            return formDataString.Contains("type=cpdlc");
        });

        Assert.NotNull(sendRequest);
        var formData = await ParseFormDataFromRequest(sendRequest);
        Assert.Equal("/data2/2/5/NE/ROGER", formData["packet"]);
    }

    [Theory]
    [InlineData(CpdlcUplinkResponseType.NoResponse, "NE")]
    [InlineData(CpdlcUplinkResponseType.WilcoUnable, "WU")]
    [InlineData(CpdlcUplinkResponseType.AffirmativeNegative, "AN")]
    [InlineData(CpdlcUplinkResponseType.Roger, "R")]
    public async Task Send_CpdlcMessage_MapsResponseTypesCorrectly(CpdlcUplinkResponseType uplinkResponseType, string expectedCode)
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var clock = new TestClock();
        var client = CreateClient(httpHandler, clock);

        await client.Connect(CancellationToken.None);

        var message = new UplinkMessage(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            uplinkResponseType,
            AlertType.None,
            "TEST",
            clock.UtcNow());

        // Act
        await client.Send(message, CancellationToken.None);

        // Assert
        var sendRequest = httpHandler.Requests.FirstOrDefault(r =>
        {
            var formDataString = r.Content?.ReadAsStringAsync().Result ?? "";
            return formDataString.Contains("type=cpdlc");
        });

        Assert.NotNull(sendRequest);
        var formData = await ParseFormDataFromRequest(sendRequest);
        Assert.Equal($"/data2/1//{expectedCode}/TEST", formData["packet"]);

        await client.DisposeAsync();
        _clients.Remove(client);
    }

    [Fact]
    public async Task Send_CpdlcMessage_TranslatesContent()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var clock = new TestClock();
        var client = CreateClient(httpHandler);

        await client.Connect(CancellationToken.None);

        var message = new UplinkMessage(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.NoResponse,
            AlertType.None,
            "MONITOR ML CEN 123.450. END SERVICE.",
            clock.UtcNow());

        // Act
        await client.Send(message, CancellationToken.None);

        // Assert
        var sendRequest = httpHandler.Requests.FirstOrDefault(r =>
        {
            var formDataString = r.Content?.ReadAsStringAsync().Result ?? "";
            return formDataString.Contains("type=cpdlc");
        });

        Assert.NotNull(sendRequest);
        var formData = await ParseFormDataFromRequest(sendRequest);
        Assert.Equal("/data2/1//NE/MONITOR+ML+CEN+123.450.+LOGOFF.", formData["packet"]);
    }

    [Fact]
    public async Task Send_CpdlcMessage_UrlEncodesContent()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var clock = new TestClock();
        var client = CreateClient(httpHandler, clock);

        await client.Connect(CancellationToken.None);

        var message = new UplinkMessage(
            1,
            null,
            "UAL123",
            "BN-TSN_FSS",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLIMB TO @FL350@",
            clock.UtcNow());

        // Act
        await client.Send(message, CancellationToken.None);

        // Assert
        var sendRequest = httpHandler.Requests.FirstOrDefault(r =>
        {
            var formDataString = r.Content?.ReadAsStringAsync().Result ?? "";
            return formDataString.Contains("type=cpdlc");
        });

        Assert.NotNull(sendRequest);
        var formData = await ParseFormDataFromRequest(sendRequest);
        Assert.Equal("/data2/1//WU/CLIMB+TO+@FL350@", formData["packet"]);
    }

    [Fact]
    public async Task Poll_CpdlcMessage_ParsesCorrectly()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.QueueResponse(HttpStatusCode.OK, "ok {UAL123 cpdlc {/data2/5//Y/REQUEST DESCENT}}");
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var client = CreateClient(httpHandler);

        // Act
        await client.Connect(CancellationToken.None);
        await Task.Delay(100); // Give polling task time to start

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var message = await client.MessageReader.ReadAsync(cts.Token);

        // Assert
        var cpdlcMessage = Assert.IsType<DownlinkMessage>(message);
        Assert.Equal("UAL123", cpdlcMessage.Sender);
        Assert.Equal(5, cpdlcMessage.MessageId);
        Assert.Equal("REQUEST DESCENT", cpdlcMessage.Content);
        Assert.Equal(CpdlcDownlinkResponseType.ResponseRequired, cpdlcMessage.ResponseType);
    }

    [Fact]
    public async Task Poll_CpdlcReply_ParsesCorrectly()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.QueueResponse(HttpStatusCode.OK, "ok {UAL123 cpdlc {/data2/7/3/N/WILCO}}");
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var client = CreateClient(httpHandler);

        // Act
        await client.Connect(CancellationToken.None);
        await Task.Delay(100); // Give polling task time to start

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var message = await client.MessageReader.ReadAsync(cts.Token);

        // Assert
        var cpdlcReply = Assert.IsType<DownlinkMessage>(message);
        Assert.Equal("UAL123", cpdlcReply.Sender);
        Assert.Equal(7, cpdlcReply.MessageId);
        Assert.Equal(3, cpdlcReply.MessageReference);
        Assert.Equal("WILCO", cpdlcReply.Content);
        Assert.Equal(CpdlcDownlinkResponseType.NoResponse, cpdlcReply.ResponseType);
    }

    [Fact]
    public async Task Poll_MultipleMessages_ParsesAllCorrectly()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        var response = "ok {UAL123 cpdlc {/data2/1//Y/REQUEST DESCENT}} {DAL456 cpdlc {/data2/2//Y/REQUEST CLIMB}}";
        httpHandler.QueueResponse(HttpStatusCode.OK, response);
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var client = CreateClient(httpHandler);

        // Act
        await client.Connect(CancellationToken.None);
        await Task.Delay(100); // Give polling task time to start

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var message1 = await client.MessageReader.ReadAsync(cts.Token);
        var message2 = await client.MessageReader.ReadAsync(cts.Token);

        // Assert
        var cpdlcMessage1 = Assert.IsType<DownlinkMessage>(message1);
        Assert.Equal("UAL123", cpdlcMessage1.Sender);
        Assert.Equal("REQUEST DESCENT", cpdlcMessage1.Content);

        var cpdlcMessage2 = Assert.IsType<DownlinkMessage>(message2);
        Assert.Equal("DAL456", cpdlcMessage2.Sender);
        Assert.Equal("REQUEST CLIMB", cpdlcMessage2.Content);
    }

    [Fact]
    public async Task Poll_MessageWithSeparator_ParsesCorrectly()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        var response = "ok {UAL123 cpdlc {/data2/1//Y/REQUEST CLIMB DUE TO A/C PERFORMANCE}}";
        httpHandler.QueueResponse(HttpStatusCode.OK, response);
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var client = CreateClient(httpHandler);

        // Act
        await client.Connect(CancellationToken.None);
        await Task.Delay(100); // Give polling task time to start

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var message = await client.MessageReader.ReadAsync(cts.Token);

        // Assert
        var cpdlcMessage = Assert.IsType<DownlinkMessage>(message);
        Assert.Equal("REQUEST CLIMB DUE TO A/C PERFORMANCE", cpdlcMessage.Content);
    }

    [Theory]
    [InlineData("N", CpdlcDownlinkResponseType.NoResponse)]
    [InlineData("Y", CpdlcDownlinkResponseType.ResponseRequired)]
    public async Task Poll_ResponseTypeCodes_MapCorrectly(string code, CpdlcDownlinkResponseType expectedType)
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.QueueResponse(HttpStatusCode.OK, $"ok {{UAL123 cpdlc {{/data2/1//{code}/TEST}}}}");
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var client = CreateClient(httpHandler);

        // Act
        await client.Connect(CancellationToken.None);
        await Task.Delay(100); // Give polling task time to start

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var message = await client.MessageReader.ReadAsync(cts.Token);

        // Assert
        var cpdlcMessage = Assert.IsType<DownlinkMessage>(message);
        Assert.Equal(expectedType, cpdlcMessage.ResponseType);

        await client.DisposeAsync();
        _clients.Remove(client);
    }

    [Fact]
    public async Task Poll_WithoutMessageReferenceNumbers_MapsCorrectly()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.QueueResponse(HttpStatusCode.OK, $"ok {{UAL123 cpdlc {{/data2///N/LOGOFF}}}}");
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var client = CreateClient(httpHandler);

        // Act
        await client.Connect(CancellationToken.None);
        await Task.Delay(100); // Give polling task time to start

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var message = await client.MessageReader.ReadAsync(cts.Token);

        // Assert
        var cpdlcMessage = Assert.IsType<DownlinkMessage>(message);
        Assert.Equal(-1, cpdlcMessage.MessageId);

        await client.DisposeAsync();
        _clients.Remove(client);
    }

    [Fact]
    public async Task ListConnections_ReturnsCallsigns()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.SetResponse(HttpStatusCode.OK, "ok {UAL123 DAL456 AAL789}");
        var client = CreateClient(httpHandler);

        // Act
        var callsigns = await client.ListConnections(CancellationToken.None);

        // Assert
        Assert.Equal(3, callsigns.Length);
        Assert.Contains("UAL123", callsigns);
        Assert.Contains("DAL456", callsigns);
        Assert.Contains("AAL789", callsigns);

        var listRequest = httpHandler.Requests.FirstOrDefault(r =>
        {
            var formDataString = r.Content?.ReadAsStringAsync().Result ?? "";
            return formDataString.Contains("type=ping");
        });

        Assert.NotNull(listRequest);
        var formData = await ParseFormDataFromRequest(listRequest);
        Assert.Equal("ping", formData["type"]);
        Assert.Equal("ALL-CALLSIGNS", formData["packet"]);
        Assert.Equal("SERVER", formData["to"]);
    }

    [Fact]
    public async Task ListConnections_EmptyResponse_ReturnsEmptyArray()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var client = CreateClient(httpHandler);

        // Act
        var callsigns = await client.ListConnections(CancellationToken.None);

        // Assert
        Assert.Empty(callsigns);
    }

    private async Task<Dictionary<string, string>> ParseFormDataFromRequest(HttpRequestMessage request)
    {
        var formDataString = await request.Content!.ReadAsStringAsync();
        var result = new Dictionary<string, string>();
        var pairs = formDataString.Split('&');

        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2)
            {
                result[Uri.UnescapeDataString(keyValue[0])] = Uri.UnescapeDataString(keyValue[1]);
            }
        }

        return result;
    }
}
