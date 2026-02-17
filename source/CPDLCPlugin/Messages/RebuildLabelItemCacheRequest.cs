using CPDLCServer.Contracts;
using MediatR;
using vatsys;
using vatsys.Plugin;

namespace CPDLCPlugin.Messages;

public record CustomStripOrLabelItem(
    string Text,
    CustomColour? BackgroundColour,
    CustomColour? ForegroundColour,
    Action LeftClickCallback);

public record RebuildLabelItemCacheRequest : IRequest;

public class RebuildLabelItemCacheRequestHandler(
    LabelItemCache labelItemCache,
    ColourCache colourCache,
    DialogueStore dialogueStore,
    AircraftConnectionStore aircraftConnectionStore,
    SuspendedMessageStore suspendedMessageStore,
    IJurisdictionChecker jurisdictionChecker,
    IMediator mediator,
    IGuiInvoker guiInvoker,
    IErrorReporter errorReporter,
    Plugin plugin) : IRequestHandler<RebuildLabelItemCacheRequest>
{
    public async Task Handle(RebuildLabelItemCacheRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var newLabelItems = new Dictionary<string, CustomStripOrLabelItem>();

            var connectedAircraft = await aircraftConnectionStore.All(cancellationToken);

            if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
            {
                labelItemCache.Clear();
                return;
            }

            foreach (var flightDataRecord in FDP2.GetFDRs)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (flightDataRecord is null)
                    continue;

                var connection = connectedAircraft.FirstOrDefault(c => c.Callsign == flightDataRecord.Callsign && c.StationId == plugin.ConnectionManager.StationIdentifier);

                var openDialogues = (await dialogueStore.All(cancellationToken))
                    .Where(d => d.AircraftCallsign == flightDataRecord.Callsign && !d.IsClosed)
                    .ToArray();

                if (cancellationToken.IsCancellationRequested)
                    return;

                var hasOpenDownlinkMessages = openDialogues
                    .SelectMany(d => d.Messages)
                    .OfType<DownlinkMessageDto>()
                    .Any(m => !m.IsClosed);

                // TODO: Check if they're connected to the ACARS network. Equipment flags are unreliable.
                var isEquipped = new[]
                {
                    "J1",
                    "J2",
                    "J3",
                    "J4",
                    "J5",
                    "J6",
                    "J7",
                }.Any(s => flightDataRecord.AircraftEquip.Contains(s));

                var unacknowledgedUnableReceived = openDialogues
                    .SelectMany(d => d.Messages)
                    .OfType<DownlinkMessageDto>()
                    .Any(m => m.Content.Contains("UNABLE") && m.Acknowledged is null);

                var hasSuspendedMessage = suspendedMessageStore.HasSuspendedMessage(flightDataRecord.Callsign);

                var text = " ";
                CustomColour? backgroundColour = null;
                CustomColour? foregroundColour = null;
                Action leftClickAction = () => { };

                if (isEquipped && connection is null)
                {
                    text = ".";
                    // TODO: Left click will initiate a manual connection
                }
                else if (connection is not null && connection.DataAuthorityState == DataAuthorityState.NextDataAuthority)
                {
                    text = "-";
                    leftClickAction = () =>
                    {
                        try
                        {
                            mediator.Send(new OpenEditorWindowRequest(flightDataRecord.Callsign));
                        }
                        catch (Exception ex)
                        {
                            errorReporter.ReportError(ex, "Error opening CPDLC Window");
                        }
                    };
                }
                else if (connection is not null && connection.DataAuthorityState == DataAuthorityState.CurrentDataAuthority)
                {
                    text = "+";
                    leftClickAction = () =>
                    {
                        try
                        {
                            mediator.Send(new OpenEditorWindowRequest(flightDataRecord.Callsign));
                        }
                        catch (Exception ex)
                        {
                            errorReporter.ReportError(ex, "Error opening CPDLC window");
                        }
                    };

                    // Color only changes if it's relevent to us
                    if (openDialogues.Any(jurisdictionChecker.ShouldDisplayDialogue))
                    {
                        if (unacknowledgedUnableReceived)
                        {
                            backgroundColour = colourCache.UnableBackgroundColour;
                        }
                        else if (hasOpenDownlinkMessages)
                        {
                            backgroundColour = colourCache.DownlinkBackgroundColour;
                        }
                    }

                    if (hasSuspendedMessage)
                    {
                        foregroundColour = colourCache.SuspendedForegroundColour;
                    }
                }

                newLabelItems[flightDataRecord.Callsign] = new CustomStripOrLabelItem(
                    text,
                    backgroundColour,
                    foregroundColour,
                    leftClickAction);
            }

            labelItemCache.Replace(newLabelItems);
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex);
        }
    }
}
