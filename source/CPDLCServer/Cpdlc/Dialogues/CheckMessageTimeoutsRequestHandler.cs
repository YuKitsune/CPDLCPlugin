using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Infrastructure;
using MediatR;

namespace CPDLCServer.Cpdlc.Dialogues;

public class CheckMessageTimeoutsRequestHandler(
    IDialogueRepository dialogueRepository,
    IClock clock,
    IPublisher publisher,
    ILogger logger)
    : IRequestHandler<CheckMessageTimeoutsRequest>
{
    readonly TimeSpan _pilotLateTimeout = TimeSpan.FromMinutes(2);
    readonly TimeSpan _controllerLateTimeout = TimeSpan.FromMinutes(2);

    public async Task Handle(CheckMessageTimeoutsRequest request, CancellationToken cancellationToken)
    {
        // TODO: Time out entire dialogues when they take too long
        // TODO: Verify if this is supposed to apply to message closures or acknowledgements.

        var now = clock.UtcNow();
        var dialogues = await dialogueRepository.FindOpen(cancellationToken);

        logger.Verbose("Checking for timeouts in {DialogueCount} dialogues", dialogues.Length);

        foreach (var dialogue in dialogues)
        {
            var anyChanges = false;
            foreach (var message in dialogue.Messages)
            {
                switch (message)
                {
                    case UplinkMessage uplink:
                        if (CheckUplinkTimeout(uplink, now))
                        {
                            anyChanges = true;
                        }
                        break;

                    case DownlinkMessage downlink:
                        if (CheckDownlinkTimeout(downlink, now))
                        {
                            anyChanges = true;
                        }
                        break;
                }
            }

            if (anyChanges)
            {
                await publisher.Publish(new DialogueChangedNotification(dialogue), cancellationToken);
            }
        }
    }

    bool CheckUplinkTimeout(UplinkMessage uplink, DateTimeOffset now)
    {
        // Skip if already closed, in timeout/failed state, or doesn't require response
        if (uplink.IsClosed ||
            uplink.IsPilotLate ||
            uplink.IsTransmissionFailed ||
            uplink.ResponseType == CpdlcUplinkResponseType.NoResponse)
            return false;

        // Check if timeout has been exceeded
        var timeSinceSent = now - uplink.Sent;
        if (timeSinceSent < _pilotLateTimeout)
            return false;

        logger.Verbose("Uplink message {UplinkId} marked as pilot late (time since sent: {TimeSinceSent}s)",
            uplink.MessageId, timeSinceSent.TotalSeconds);
        uplink.IsPilotLate = true;
        return true;
    }

    bool CheckDownlinkTimeout(DownlinkMessage downlink, DateTimeOffset now)
    {
        // Skip if already closed, in timeout state, or doesn't require response
        if (downlink.IsClosed ||
            downlink.IsControllerLate ||
            downlink.ResponseType == CpdlcDownlinkResponseType.NoResponse)
            return false;

        // Check if timeout has been exceeded
        var timeSinceReceived = now - downlink.Received;
        if (timeSinceReceived < _controllerLateTimeout)
            return false;

        logger.Verbose("Downlink message {DownlinkId} marked as controller late (time since received: {TimeSinceReceived}s)",
            downlink.MessageId, timeSinceReceived.TotalSeconds);
        downlink.IsControllerLate = true;
        return true;
    }
}
