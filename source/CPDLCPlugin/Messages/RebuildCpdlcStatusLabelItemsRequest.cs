using CPDLCPlugin.LabelItems;
using CPDLCServer.Contracts;
using MediatR;
using vatsys;
using vatsys.Plugin;

namespace CPDLCPlugin.Messages;

public record RebuildCpdlcStatusLabelItemsRequest : IRequest;

public class RebuildCpdlcStatusLabelItemsRequestHandler(
    CpdlcStatusLabelItemProvider cpdlcStatusProvider,
    ColourCache colourCache,
    DialogueStore dialogueStore,
    AircraftConnectionStore aircraftConnectionStore,
    AcarsConnectedCallsignStore acarsConnectedCallsignStore,
    SuspendedMessageStore suspendedMessageStore,
    IJurisdictionChecker jurisdictionChecker,
    IMediator mediator,
    IErrorReporter errorReporter,
    Plugin plugin) : IRequestHandler<RebuildCpdlcStatusLabelItemsRequest>
{
    public async Task Handle(RebuildCpdlcStatusLabelItemsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var newLabelItems = new Dictionary<string, CustomLabelItem>();

            var connectedAircraft = await aircraftConnectionStore.All(cancellationToken);

            if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
            {
                cpdlcStatusProvider.Clear();
                return;
            }

            foreach (var flightDataRecord in FDP2.GetFDRs)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (flightDataRecord is null)
                    continue;

                try
                {
                    var callsign = flightDataRecord.Callsign;
                    if (string.IsNullOrEmpty(callsign))
                        continue;

                    var connection = connectedAircraft.FirstOrDefault(c => c.Callsign == callsign && c.StationId == plugin.ConnectionManager.StationIdentifier);

                    var openDialogues = (await dialogueStore.All(cancellationToken))
                        .Where(d => d.AircraftCallsign == callsign && !d.IsClosed)
                        .ToArray();

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    var hasOpenDownlinkMessages = openDialogues
                        .SelectMany(d => d.Messages ?? [])
                        .OfType<DownlinkMessageDto>()
                        .Any(m => !m.IsClosed);

                    var isOnAcarsNetwork = await acarsConnectedCallsignStore.IsConnected(callsign, cancellationToken);

                    var unacknowledgedUnableReceived = openDialogues
                        .SelectMany(d => d.Messages ?? [])
                        .OfType<DownlinkMessageDto>()
                        .Any(m => (m.Content ?? string.Empty).Contains("UNABLE") && m.Acknowledged is null);

                    var hasSuspendedMessage = suspendedMessageStore.HasSuspendedMessage(callsign);

                    var text = " ";
                    CustomColour? backgroundColour = null;
                    CustomColour? foregroundColour = null;
                    Action<CustomLabelItemMouseClickEventArgs>? onMouseClick = null;

                    if (isOnAcarsNetwork && connection is null)
                    {
                        text = ".";
                        // TODO: Left click will initiate a manual connection
                    }
                    else if (connection is not null && connection.DataAuthorityState == DataAuthorityState.NextDataAuthority)
                    {
                        text = "-";
                        onMouseClick = args =>
                        {
                            try
                            {
                                if (args.Button != CustomLabelItemMouseButton.Left)
                                    return;

                                mediator.Send(new OpenEditorWindowRequest(callsign));
                                args.Handled = true;
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
                        onMouseClick = args =>
                        {
                            try
                            {
                                if (args.Button != CustomLabelItemMouseButton.Left)
                                    return;

                                mediator.Send(new OpenEditorWindowRequest(callsign));
                                args.Handled = true;
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

                    var labelItem = new CustomLabelItem
                    {
                        Type = "CPDLCPLUGIN_CPDLCSTATUS",
                        Text = text
                    };

                    if (backgroundColour is not null)
                    {
                        labelItem.BackColourIdentity = Colours.Identities.Custom;
                        labelItem.CustomBackColour = backgroundColour;
                    }

                    if (foregroundColour is not null)
                    {
                        labelItem.ForeColourIdentity = Colours.Identities.Custom;
                        labelItem.CustomForeColour = foregroundColour;
                    }

                    if (onMouseClick is not null)
                    {
                        labelItem.OnMouseClick = onMouseClick;
                    }

                    newLabelItems[callsign] = labelItem;
                }
                catch (Exception ex)
                {
                    // Log but continue processing other aircraft
                    errorReporter.ReportError(ex, $"Error processing label for {flightDataRecord.Callsign ?? "unknown"}");
                }
            }

            cpdlcStatusProvider.Replace(newLabelItems);
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex);
        }
    }
}
