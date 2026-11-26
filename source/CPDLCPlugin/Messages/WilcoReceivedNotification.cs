using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using CPDLCServer.Contracts;
using HarmonyLib;
using MediatR;
using Serilog;
using vatsys;
using vatSysToolbox;

namespace CPDLCPlugin.Messages;

// Published when the pilot sends WILCO to an uplink.
// A handler can update the FDR from the uplink, and then send MarkUplinkActionedRequest.
public record WilcoReceivedNotification(DialogueDto Dialogue, UplinkMessageDto Uplink) : INotification;

// Updates the FDR when the pilot sends WILCO to certain instructions
public class WilcoReceivedNotificationHandler(IMediator mediator, IGuiInvoker guiInvoker, ILogger logger)
    : INotificationHandler<WilcoReceivedNotification>
{
    // Three digits are hundreds of feet, for example "350", "F350" and "FL350" are 35000 ft
    static readonly Regex HundredsOfFeetRegex = new(@"^(?:FL|F|A)?\s?(\d{3})$");
    static readonly Regex FeetRegex = new(@"^(\d{4,5})\s?(?:FT)?$");

    // The vatSys method that the Direct To menu uses. It changes the route to go direct from the present position.
    const string DirectToMethodName = "vatsys.MMI:DirectToMenuModifyRoute";

    public async Task Handle(WilcoReceivedNotification notification, CancellationToken cancellationToken)
    {
        var uplink = notification.Uplink;
        if (!uplink.CanAction || uplink.Actioned)
            return;

        // Only the controller that tracks the aircraft updates the FDR
        var fdr = FDP2.GetFDRs.FirstOrDefault(f => f.Callsign == notification.Dialogue.AircraftCallsign);
        if (fdr is null || !fdr.IsTrackedByMe)
            return;

        // Each change is applied when it can be, but the uplink is actioned only when all changes are applied
        var directToApplied = TryApplyDirectTo(fdr, uplink);
        var levelApplied = TryApplyLevel(fdr, uplink);
        if (!directToApplied || !levelApplied)
            return;

        await mediator.Send(new MarkUplinkActionedRequest(notification.Dialogue.Id, uplink.MessageId), cancellationToken);
    }

    // Returns false if the uplink has a direct to instruction that cannot be applied
    bool TryApplyDirectTo(FDP2.FDR fdr, UplinkMessageDto uplink)
    {
        // The last direct to instruction in the uplink is the one that applies
        var element = uplink.Elements.LastOrDefault(e => ActionableElements.DirectTo.Contains(e.Id));
        if (element is null)
            return true;

        if (!TryFindWaypointAhead(fdr, element.Parameters[0], out var waypoint))
        {
            logger.Warning("Cannot find {Waypoint} in the route of {Callsign}", element.Parameters[0], fdr.Callsign);
            return false;
        }

        guiInvoker.InvokeOnGUI(_ => DirectTo(fdr, waypoint));
        logger.Information("Set the route of {Callsign} direct to {Waypoint} from uplink {MessageId}", fdr.Callsign, waypoint.Intersection.Name, uplink.MessageId);
        return true;
    }

    // Returns false if the uplink has a level instruction that cannot be applied
    bool TryApplyLevel(FDP2.FDR fdr, UplinkMessageDto uplink)
    {
        // The last level instruction in the uplink is the one that applies
        var element = uplink.Elements.LastOrDefault(e =>
            ActionableElements.Level.Contains(e.Id) || ActionableElements.BlockLevel.Contains(e.Id));
        if (element is null)
            return true;

        var lowerLevel = -1;
        int upperLevel;
        if (ActionableElements.BlockLevel.Contains(element.Id))
        {
            if (!TryParseAltitude(element.Parameters[0], out lowerLevel) ||
                !TryParseAltitude(element.Parameters[1], out upperLevel))
            {
                logger.Warning("Cannot read the block level {Lower} to {Upper}", element.Parameters[0], element.Parameters[1]);
                return false;
            }
        }
        else
        {
            if (!TryParseAltitude(element.Parameters[0], out upperLevel))
            {
                logger.Warning("Cannot read the level {Level}", element.Parameters[0]);
                return false;
            }
        }

        guiInvoker.InvokeOnGUI(_ => FDP2.SetCFL(fdr, lowerLevel, upperLevel, false));
        logger.Information("Set the CFL of {Callsign} from uplink {MessageId}", fdr.Callsign, uplink.MessageId);
        return true;
    }

    // Finds the waypoint in the part of the route that the aircraft has not flown yet.
    // This is the same list of waypoints that the vatSys Direct To menu shows.
    static bool TryFindWaypointAhead(
        FDP2.FDR fdr,
        string waypointName,
        [NotNullWhen(true)] out FDP2.FDR.ExtractedRoute.Segment? waypoint)
    {
        var name = waypointName.Trim().ToUpperInvariant();
        var overflownIndex = Math.Max(fdr.ParsedRoute.OverflownIndex, 0);

        waypoint = fdr.ParsedRoute.ToList()
            .Skip(overflownIndex + 1)
            .Where(s => s.Type == FDP2.FDR.ExtractedRoute.Segment.SegmentTypes.WAYPOINT)
            .FirstOrDefault(s => s.Intersection.Name == name);

        return waypoint is not null;
    }

    static void DirectTo(FDP2.FDR fdr, FDP2.FDR.ExtractedRoute.Segment waypoint)
    {
        var method = AccessTools.Method(DirectToMethodName)
            ?? throw new MissingMethodException($"Cannot find the vatSys method {DirectToMethodName}");

        // A null "from" segment makes vatSys start the new route at the present position
        method.Invoke(null, [fdr, null, waypoint]);
    }

    // Gets the altitude in feet, for example "350" and "FL350" are 35000, and "5000FT" is 5000
    static bool TryParseAltitude(string altitude, out int feet)
    {
        var text = altitude.Trim().ToUpperInvariant();

        var hundredsOfFeetMatch = HundredsOfFeetRegex.Match(text);
        if (hundredsOfFeetMatch.Success)
        {
            feet = int.Parse(hundredsOfFeetMatch.Groups[1].Value) * 100;
            return true;
        }

        var feetMatch = FeetRegex.Match(text);
        if (feetMatch.Success)
        {
            feet = int.Parse(feetMatch.Groups[1].Value);
            return true;
        }

        feet = -1;
        return false;
    }
}
