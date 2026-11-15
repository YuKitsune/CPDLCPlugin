using CPDLCServer.AcarsConnections;
using CPDLCServer.Cpdlc.Uplinks;

namespace CPDLCServer.Tests.Mocks;

// Sends each uplink immediately, so that the handler tests can check the messages after the handler returns
public class TestUplinkTransmitter : IUplinkTransmitter
{
    public void TransmitAfterCommit(IAcarsConnection acarsConnection, UplinkMessage uplink)
    {
        acarsConnection.Send(uplink, CancellationToken.None).GetAwaiter().GetResult();
    }
}
