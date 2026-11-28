using System.Threading.Channels;
using CPDLCServer.AcarsConnections;
using CPDLCServer.AdscContracts;
using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;

namespace CPDLCServer.Tests.Mocks;

public class TestAcarsConnection(string stationId) : IAcarsConnection
{
    private readonly Channel<ReceivedDownlink> _channel = Channel.CreateUnbounded<ReceivedDownlink>();
    private readonly Channel<ReceivedAdscMessage> _adscChannel = Channel.CreateUnbounded<ReceivedAdscMessage>();
    private readonly Channel<ReceivedControlMessage> _controlChannel = Channel.CreateUnbounded<ReceivedControlMessage>();

    public ChannelReader<ReceivedDownlink> MessageReader => _channel.Reader;
    public ChannelReader<ReceivedAdscMessage> AdscMessageReader => _adscChannel.Reader;
    public ChannelReader<ReceivedControlMessage> ControlMessageReader => _controlChannel.Reader;
    public string StationId => stationId;

    public List<string> Connections { get; } = [];
    public List<UplinkMessage> SentMessages { get; } = new();
    public List<(string Callsign, int MessageId)> SentConnectionRequests { get; } = [];
    public List<(string Callsign, int MessageId)> AcceptedLogons { get; } = [];
    public List<(string Callsign, int MessageId)> RejectedLogons { get; } = [];
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

    public Task SendConnectionRequest(string callsign, int messageId, CancellationToken cancellationToken)
    {
        SentConnectionRequests.Add((callsign, messageId));
        return Task.CompletedTask;
    }

    public Task AcceptLogon(string callsign, int messageId, CancellationToken cancellationToken)
    {
        AcceptedLogons.Add((callsign, messageId));
        return Task.CompletedTask;
    }

    public Task RejectLogon(string callsign, int messageId, CancellationToken cancellationToken)
    {
        RejectedLogons.Add((callsign, messageId));
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
