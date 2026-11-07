using System.Threading.Channels;
using CPDLCServer.Contracts;
using CPDLCServer.AdscContracts;
using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;

namespace CPDLCServer.AcarsConnections;

public interface IAcarsConnection : IAsyncDisposable
{
    ChannelReader<ReceivedDownlink> MessageReader { get; }
    ChannelReader<ReceivedAdscMessage> AdscMessageReader { get; }
    string StationId { get; }
    Task Connect(CancellationToken cancellationToken);
    Task Send(UplinkMessage message, CancellationToken cancellationToken);
    Task RequestPeriodicAdscContract(string callsign, TimeSpan reportingInterval, CancellationToken cancellationToken);
    Task RequestAdscDemandReport(string callsign, CancellationToken cancellationToken);
    Task CancelAdscContract(string callsign, CancellationToken cancellationToken);
    Task<string[]> ListConnections(CancellationToken cancellationToken);
    Task Disconnect(CancellationToken cancellationToken);
}
