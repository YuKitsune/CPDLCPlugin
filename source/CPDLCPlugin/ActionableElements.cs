namespace CPDLCPlugin;

// The uplink elements that the plugin can use to update the FDR when the pilot sends WILCO.
// When an uplink has one of these elements, the uplink is sent with CanAction, and the "X" prefix shows until
// the FDR is updated.
public static class ActionableElements
{
    static readonly HashSet<int> ElementIds =
    [
        19, // MAINTAIN [altitude]
        20, // CLIMB TO AND MAINTAIN [altitude]
        23, // DESCEND TO AND MAINTAIN [altitude]
        30, // MAINTAIN BLOCK [altitude] TO [altitude]
        31, // CLIMB TO AND MAINTAIN BLOCK [altitude] TO [altitude]
        32, // DESCEND TO AND MAINTAIN BLOCK [altitude] TO [altitude]
        36, // EXPEDITE CLIMB TO [altitude]
        37, // EXPEDITE DESCENT TO [altitude]
        38, // IMMEDIATELY CLIMB TO [altitude]
        39, // IMMEDIATELY DESCEND TO [altitude]
        74, // PROCEED DIRECT TO [position]
        75, // WHEN ABLE PROCEED DIRECT TO [position]
        79, // CLEARED TO [position] VIA [route clearance]
        80  // CLEARED [route clearance]
    ];

    public static bool Contains(int uplinkElementId) => ElementIds.Contains(uplinkElementId);
}
