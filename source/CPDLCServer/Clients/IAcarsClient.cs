using System.Threading.Channels;
using CPDLCServer.Contracts;
using CPDLCServer.Model;

namespace CPDLCServer.Clients;

public interface IAcarsClient : IAsyncDisposable
{
    ChannelReader<ReceivedDownlink> MessageReader { get; }
    string StationId { get; }
    Task Connect(CancellationToken cancellationToken);
    Task Send(UplinkMessage message, CancellationToken cancellationToken);
    Task<string[]> ListConnections(CancellationToken cancellationToken);
    Task Disconnect(CancellationToken cancellationToken);
}
