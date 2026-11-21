using CPDLCServer.AcarsConnections;
using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Cpdlc.Motd;

// When an aircraft logs on, the message of the day of the ACARS connection is sent after its delay.
// It is sent only to the current data authority, so that the next data authority does not send it a second time.
public class SendMotdOnConnectionEstablishedHandler(
    IAcarsConnectionManager acarsConnectionManager,
    MotdScheduler motdScheduler,
    IUnitOfWork unitOfWork)
    : INotificationHandler<AircraftConnectionEstablished>
{
    public async Task Handle(AircraftConnectionEstablished notification, CancellationToken cancellationToken)
    {
        if (notification.DataAuthorityState != DataAuthorityState.CurrentDataAuthority)
            return;

        var configuration = await acarsConnectionManager.GetConfigurationForStation(notification.StationId, cancellationToken);
        if (string.IsNullOrWhiteSpace(configuration.Motd))
            return;

        unitOfWork.AfterCommit(_ =>
        {
            motdScheduler.Schedule(notification.AcarsConnectionId, notification.Callsign, configuration.MotdDelay);
            return Task.CompletedTask;
        });
    }
}
