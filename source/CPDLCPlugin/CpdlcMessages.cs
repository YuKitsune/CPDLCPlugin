namespace CPDLCPlugin;

/// <summary>
/// The messages that other plugins (for example, the buttons of the Eurocat Menus plugin) use to control the CPDLC
/// plugin. Send these messages to the mailbox <see cref="Plugin.MailboxName"/>. The plugin ignores the payload.
/// </summary>
public static class CpdlcMessages
{
    /// <summary>
    /// Opens or closes the CPDLC Setup window.
    /// </summary>
    public const string ToggleSetupWindow = "dev.eoinmotherway.cpdlc.toggle-setup-window";

    /// <summary>
    /// Opens or closes the ADS Contract window for the selected track.
    /// </summary>
    public const string ToggleAdsContractWindow = "dev.eoinmotherway.cpdlc.toggle-ads-contract-window";

    /// <summary>
    /// Opens or closes the CPDLC History window for the selected track.
    /// </summary>
    public const string ToggleHistoryWindow = "dev.eoinmotherway.cpdlc.toggle-history-window";

    // The plugin sends these messages to all plugins when a window opens or closes. The payload is an empty JSON object.

    public const string SetupWindowOpened = "dev.eoinmotherway.cpdlc.setup-window-opened";
    public const string SetupWindowClosed = "dev.eoinmotherway.cpdlc.setup-window-closed";
    public const string AdsContractWindowOpened = "dev.eoinmotherway.cpdlc.ads-contract-window-opened";
    public const string AdsContractWindowClosed = "dev.eoinmotherway.cpdlc.ads-contract-window-closed";
    public const string HistoryWindowOpened = "dev.eoinmotherway.cpdlc.history-window-opened";
    public const string HistoryWindowClosed = "dev.eoinmotherway.cpdlc.history-window-closed";
}
