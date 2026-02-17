using System.Collections.Concurrent;
using CPDLCServer.Contracts;
using vatsys;

namespace CPDLCPlugin;

public interface IJurisdictionChecker
{
    void RecordFdrOwner(string callsign, string? controllerCallsign);
    OwnershipRecord GetOwnershipRecord(string callsign);
    bool ShouldDisplayDialogue(DialogueDto dialogue);
    bool ShouldDisplayDialogue(DialogueDto dialogue, FDP2.FDR fdr);
}

public record OwnershipRecord(string? CurrentOwner, string? PreviousOwner);

public class JurisdictionChecker(ControllerConnectionStore controllerConnectionStore) : IJurisdictionChecker
{
    // Need to keep track of which controller last had ownership of each FDR
    // vatSys will set the owner to `null` when the tag is relinquished, and there's no reference to who "previously" owned it
    readonly ConcurrentDictionary<string, OwnershipRecord> _ownershipRecords = new();

    readonly ControllerConnectionStore _controllerConnectionStore = controllerConnectionStore;

    public void RecordFdrOwner(string callsign, string? controllerCallsign)
    {
        _ownershipRecords.AddOrUpdate(
            callsign,
            new OwnershipRecord(controllerCallsign, null),
            (_, existing) => existing.CurrentOwner == controllerCallsign
                ? existing
                : new OwnershipRecord(controllerCallsign, existing.CurrentOwner));
    }

    public OwnershipRecord? GetOwnershipRecord(string callsign)
    {
        return _ownershipRecords.TryGetValue(callsign, out var record) ? record : null;
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

        if (!_ownershipRecords.TryGetValue(fdr.Callsign, out var record))
            return false;

        // If nobody has jurisdiction, and we were the last owner, show the message
        if (!fdr.IsTracked && record.CurrentOwner == Network.Callsign)
        {
            return true;
        }

        // VATSIM-ism: If the controlling sector isn't connected to the ATSU server, and we were the previous owner, then show the message
        if (fdr.ControllerTracking is not null &&
            fdr.ControllerTracking.Callsign != Network.Callsign &&
            record.PreviousOwner == Network.Callsign)
        {
            var trackingControllerIsConnected = _controllerConnectionStore.IsConnected(fdr.ControllerTracking.Callsign);
            if (!trackingControllerIsConnected)
            {
                return true;
            }
        }

        return false;
    }
}
