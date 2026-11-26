namespace CPDLCPlugin;

// The uplink elements that the plugin uses to update the FDR when the pilot sends WILCO.
// When an uplink has one of these elements, the uplink is sent with CanAction. The "X" prefix shows if the uplink
// is closed and the FDR was not updated.
// Add an element here only if WilcoReceivedNotificationHandler can apply it.
public static class ActionableElements
{
    // Level instructions with one altitude parameter
    public static readonly int[] Level =
    [
        19, // MAINTAIN [altitude]
        20, // CLIMB TO AND MAINTAIN [altitude]
        23, // DESCEND TO AND MAINTAIN [altitude]
        36, // EXPEDITE CLIMB TO [altitude]
        37, // EXPEDITE DESCENT TO [altitude]
        38, // IMMEDIATELY CLIMB TO [altitude]
        39  // IMMEDIATELY DESCEND TO [altitude]
    ];

    // Block level instructions with a lower and an upper altitude parameter
    public static readonly int[] BlockLevel =
    [
        30, // MAINTAIN BLOCK [altitude] TO [altitude]
        31, // CLIMB TO AND MAINTAIN BLOCK [altitude] TO [altitude]
        32  // DESCEND TO AND MAINTAIN BLOCK [altitude] TO [altitude]
    ];

    // Direct to instructions with one position parameter
    public static readonly int[] DirectTo =
    [
        74, // PROCEED DIRECT TO [position]
        75  // WHEN ABLE PROCEED DIRECT TO [position]
    ];

    public static bool Contains(int uplinkElementId) =>
        Level.Contains(uplinkElementId) ||
        BlockLevel.Contains(uplinkElementId) ||
        DirectTo.Contains(uplinkElementId);
}
