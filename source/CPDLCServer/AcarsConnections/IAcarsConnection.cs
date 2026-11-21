using System.Threading.Channels;
using CPDLCServer.Contracts;
using CPDLCServer.AdscContracts;
using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;

namespace CPDLCServer.AcarsConnections;

/// <summary>
///     Manages the connection to the upstream ACARS network.
/// </summary>
public interface IAcarsConnection : IAsyncDisposable
{
    /// <summary>
    ///     The identifier of the station on the ACARS network.
    /// </summary>
    string StationId { get; } // TODO: Can we get rid of this, or consider it internal to the connection?

    /// <summary>
    ///     A <see cref="ChannelReader{T}"/> where CPDLC downlink messages can be read.
    /// </summary>
    ChannelReader<ReceivedDownlink> MessageReader { get; }

    /// <summary>
    ///     A <see cref="ChannelReader{T}"/> where ADS-C messages can be read.
    /// </summary>
    ChannelReader<ReceivedAdscMessage> AdscMessageReader { get; }

    /// <summary>
    ///     Initiates the connection to the ACARS network, and starts reading messages.
    ///     Use <see cref="MessageReader"/> and <see cref="AdscMessageReader"/> to read the received messages.
    /// </summary>
    Task Connect(CancellationToken cancellationToken);

    /// <summary>
    ///     Transmits a CPDLC uplink message.
    /// </summary>
    Task Send(UplinkMessage message, CancellationToken cancellationToken);

    /// <summary>
    ///     Transmits a request for a periodic ADS contract.
    /// </summary>
    Task RequestPeriodicAdscContract(string callsign, TimeSpan reportingInterval, CancellationToken cancellationToken);

    /// <summary>
    ///     Transmits a request for an on-demand ADS report.
    /// </summary>
    Task RequestAdscDemandReport(string callsign, CancellationToken cancellationToken);

    /// <summary>
    ///     Notifies the termination of an ADS contract.
    /// </summary>
    Task CancelAdscContract(string callsign, CancellationToken cancellationToken);

    /// <summary>
    ///     Lists all clients connected to the ACARS network.
    /// </summary>
    Task<string[]> ListConnections(CancellationToken cancellationToken);

    /// <summary>
    ///     Terminates the connection to the ACARS network, and stops receiving messages.
    /// </summary>
    Task Disconnect(CancellationToken cancellationToken);
}
