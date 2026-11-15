using CPDLCServer.AcarsConnections;
using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;

namespace CPDLCServer.Cpdlc.Uplinks;

public interface IUplinkTransmitter
{
    /// <summary>
    /// Sends the uplink after the unit of work is committed.
    /// If the ACARS network does not accept the uplink, the uplink is marked as failed in a new unit of work.
    /// </summary>
    void TransmitAfterCommit(IAcarsConnection acarsConnection, UplinkMessage uplink);
}

public class UplinkTransmitter(
    IUnitOfWork unitOfWork,
    IUnitOfWorkMediator unitOfWorkMediator,
    ILogger logger)
    : IUplinkTransmitter
{
    public void TransmitAfterCommit(IAcarsConnection acarsConnection, UplinkMessage uplink)
    {
        unitOfWork.AfterCommit(async ct =>
        {
            try
            {
                await acarsConnection.Send(uplink, ct);
                logger.Information("Sent CPDLC message from {Sender} to {Recipient}", uplink.SenderCallsign, uplink.Recipient);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to send uplink {MessageId} to {Recipient}", uplink.MessageId, uplink.Recipient);

                // The unit of work of the uplink is already committed, so a new unit of work changes the uplink
                await unitOfWorkMediator.Send(
                    new MarkUplinkTransmissionFailedCommand(uplink.DialogueId, uplink.MessageId),
                    ct);

                throw;
            }
        });
    }
}
