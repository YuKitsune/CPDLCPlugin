using CPDLCPlugin.Messages;
using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCPlugin.Server;

public class MediatorMessageHandler(IMediator mediator) : IDownlinkHandlerDelegate
{
    int _isDisconnecting;

    public async Task DialogueChanged(DialogueDto dialogue, CancellationToken cancellationToken)
    {
        try
        {
            await mediator.Publish(new DialogueChangedNotification(dialogue), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            Plugin.AddError(ex, "Failed to handle DialogueChanged notification");
        }
    }

    public async Task AircraftConnectionUpdated(AircraftConnectionDto aircraftConnectionDto, CancellationToken cancellationToken)
    {
        try
        {
            await mediator.Publish(new AircraftConnectionUpdatedNotification(aircraftConnectionDto), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            Plugin.AddError(ex, "Failed to handle AircraftConnectionUpdated notification");
        }
    }

    public async Task AircraftConnectionRemoved(string callsign, string stationId, CancellationToken cancellationToken)
    {
        try
        {
            await mediator.Publish(new AircraftConnectionRemovedNotification(callsign, stationId), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            Plugin.AddError(ex, "Failed to handle AircraftConnectionRemoved notification");
        }
    }

    public async Task ControllerConnectionUpdated(ControllerConnectionDto controllerConnectionDto, CancellationToken cancellationToken)
    {
        try
        {
            await mediator.Publish(new ControllerConnectionUpdatedNotification(controllerConnectionDto), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            Plugin.AddError(ex, "Failed to handle ControllerConnectionUpdated notification");
        }
    }

    public async Task ControllerConnectionRemoved(string callsign, CancellationToken cancellationToken)
    {
        try
        {
            await mediator.Publish(new ControllerConnectionRemovedNotification(callsign), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            Plugin.AddError(ex, "Failed to handle ControllerConnectionRemoved notification");
        }
    }

    public async Task AcarsConnectedCallsignsUpdated(string[] callsigns, CancellationToken cancellationToken)
    {
        try
        {
            await mediator.Publish(new AcarsConnectedCallsignsUpdatedNotification(callsigns), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            Plugin.AddError(ex, "Failed to handle AcarsConnectedCallsignsUpdated notification");
        }
    }

    public void Error(Exception error)
    {
        Plugin.AddError(error, "Error from SignalR connection");
        if (Interlocked.CompareExchange(ref _isDisconnecting, 1, 0) == 0)
        {
            _ = DisconnectAsync();
        }
    }

    async Task DisconnectAsync()
    {
        try
        {
            await mediator.Send(new DisconnectRequest());
        }
        catch (Exception ex)
        {
            Plugin.AddError(ex, "Failed to disconnect after SignalR error");
        }
        finally
        {
            Interlocked.Exchange(ref _isDisconnecting, 0);
        }
    }
}
