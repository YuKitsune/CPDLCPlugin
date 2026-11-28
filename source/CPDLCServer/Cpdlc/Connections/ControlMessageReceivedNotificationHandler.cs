using CPDLCServer.AcarsConnections;
using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public class ControlMessageReceivedNotificationHandler(IMediator mediator, ILogger logger)
    : INotificationHandler<ControlMessageReceivedNotification>
{
    public async Task Handle(ControlMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        switch (notification.ControlMessage)
        {
            case ReceivedLogonRequest logonRequest:
                await mediator.Send(
                    new LogonCommand(logonRequest.Callsign, notification.AcarsConnectionId),
                    cancellationToken);
                break;

            // TerminateConnectionRequest does nothing if the aircraft is not known
            case ReceivedLogoff logoff:
                logger.Information("Logoff received from {Callsign}", logoff.Callsign);
                await mediator.Send(
                    new TerminateConnectionRequest(logoff.Callsign, notification.AcarsConnectionId),
                    cancellationToken);
                break;
        }
    }
}
