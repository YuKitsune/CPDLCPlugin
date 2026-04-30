using System.Threading.Channels;
using CPDLCServer.Clients;
using CPDLCServer.Model;

namespace CPDLCServer.Tests.Mocks;

public class TestAcarsClient(string stationId) : IAcarsClient
{
    private readonly Channel<DownlinkMessage> _channel = Channel.CreateUnbounded<DownlinkMessage>();

    public ChannelReader<DownlinkMessage> MessageReader => _channel.Reader;
    public string StationId => stationId;

    public List<string> Connections { get; } = [];
    public List<UplinkMessage> SentMessages { get; } = new();

    public Task Connect(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task Send(UplinkMessage message, CancellationToken cancellationToken)
    {
        SentMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task<string[]> ListConnections(CancellationToken cancellationToken)
    {
        return Task.FromResult(Connections.ToArray());
    }

    public Task Disconnect(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
