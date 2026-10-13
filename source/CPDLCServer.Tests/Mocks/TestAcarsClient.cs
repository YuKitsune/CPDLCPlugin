using System.Threading.Channels;
using CPDLCServer.Clients;
using CPDLCServer.Model;

namespace CPDLCServer.Tests.Mocks;

public class TestAcarsClient(string stationId) : IAcarsClient
{
    private readonly Channel<ReceivedDownlink> _channel = Channel.CreateUnbounded<ReceivedDownlink>();
    private readonly Channel<ReceivedAdscMessage> _adscChannel = Channel.CreateUnbounded<ReceivedAdscMessage>();

    public ChannelReader<ReceivedDownlink> MessageReader => _channel.Reader;
    public ChannelReader<ReceivedAdscMessage> AdscMessageReader => _adscChannel.Reader;
    public string StationId => stationId;

    public List<string> Connections { get; } = [];
    public List<UplinkMessage> SentMessages { get; } = new();
    public Dictionary<string, TimeSpan> RequestedAdscContracts { get; } = new();
    public List<string> RequestedAdscDemandReports { get; } = [];
    public List<string> CancelledAdscContracts { get; } = [];

    public Task Connect(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task Send(UplinkMessage message, CancellationToken cancellationToken)
    {
        SentMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task RequestPeriodicAdscContract(string callsign, TimeSpan reportingInterval, CancellationToken cancellationToken)
    {
        RequestedAdscContracts[callsign] = reportingInterval;
        return Task.CompletedTask;
    }

    public Task RequestAdscDemandReport(string callsign, CancellationToken cancellationToken)
    {
        RequestedAdscDemandReports.Add(callsign);
        return Task.CompletedTask;
    }

    public Task CancelAdscContract(string callsign, CancellationToken cancellationToken)
    {
        CancelledAdscContracts.Add(callsign);
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
