using System.Collections.Concurrent;
using CPDLCServer.Contracts;
using vatsys;

namespace CPDLCPlugin;

public interface IJurisdictionChecker
{
    void RecordFdrOwner(string callsign, string controllerCallsign);
    bool ShouldDisplayDialogue(DialogueDto dialogue);
    bool ShouldDisplayDialogue(DialogueDto dialogue, FDP2.FDR fdr);
}

public class JurisdictionChecker(ControllerConnectionStore controllerConnectionStore) : IJurisdictionChecker
{
    // Need to keep track of which controller last had ownership of each FDR
    // vatSys will set the owner to `null` when the tag is relinquished, and there's no reference to who "previously" owned it
    // Key = aircraft callsign
    // Value = controller callsign
    readonly ConcurrentDictionary<string, List<string>> _lastKnownOwners = new();

    readonly ControllerConnectionStore _controllerConnectionStore = controllerConnectionStore;

    public void RecordFdrOwner(string callsign, string controllerCallsign)
    {
        _lastKnownOwners.AddOrUpdate(
            callsign,
            [controllerCallsign],
            (_, list) => [..list, controllerCallsign]);
    }

    public bool ShouldDisplayDialogue(DialogueDto dialogue)
    {
        var fdr = FDP2.GetFDRs.FirstOrDefault(f => f.Callsign == dialogue.AircraftCallsign);
        if (fdr == null)
            return false;

        return ShouldDisplayDialogue(dialogue, fdr);
    }

    public bool ShouldDisplayDialogue(DialogueDto dialogue, FDP2.FDR fdr)
    {
        // If we have jurisdiction, show the message
        if (fdr.IsTrackedByMe)
        {
            return true;
        }

        // VATSIM-ism: If we're involved in the dialogue, then we should see the messages
        var hasSentUplink = dialogue.Messages.OfType<UplinkMessageDto>().Any(um => um.SenderCallsign == Network.Callsign);
        if (hasSentUplink)
        {
            return true;
        }

        // If nobody has jurisdiction, and we're the next owner, show the message
        var track = MMI.FindTrack(fdr);
        if (!fdr.IsTracked && track?.State == MMI.HMIStates.Announced)
        {
            return true;
        }

        if (!_lastKnownOwners.TryGetValue(fdr.Callsign, out var owners))
            return false;

        // If nobody has jurisdiction, and we were the last owner, show the message
        if (!fdr.IsTracked && owners.Last() == Network.Callsign)
        {
            return true;
        }

        // VATSIM-ism: If the controlling sector isn't connected to the ATSU server, and we were the last owner, then show the message
        if (fdr.ControllerTracking is not null && fdr.ControllerTracking.Callsign != Network.Callsign && owners.Count > 2)
        {
            var trackingControllerIsConnected = _controllerConnectionStore.IsConnected(fdr.ControllerTracking.Callsign);
            var weWereTheLastControllerBeforeThisOne = owners[owners.Count - 2] == Network.Callsign;
            if (!trackingControllerIsConnected && weWereTheLastControllerBeforeThisOne)
            {
                return true;
            }
        }

        return false;
    }
}
