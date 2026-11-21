using vatSysToolbox;
using vatSysToolbox.Messaging;
using vatSysToolbox.Windows;

namespace CPDLCPlugin;

/// <summary>
/// Tells the other plugins (for example, the CPDLC buttons of the Eurocat Menus plugin) when a window opens or closes.
/// </summary>
public class WindowEvents(IPluginMessenger messenger, IErrorReporter errorReporter)
{
    // The messages have no data
    static readonly MessageContent EmptyContent = new("application/json", "{}"u8.ToArray());

    /// <summary>
    /// Sends the opened message now, and the closed message when the window closes. Call this method when the window
    /// is made.
    /// </summary>
    public void Broadcast(IWindowHandle windowHandle, string openedMessageType, string closedMessageType)
    {
        // The handle has no Closed event. A close that no handler cancels closes the window.
        windowHandle.BeforeClose += (_, e) =>
        {
            if (!e.Cancel)
                Broadcast(closedMessageType);
        };

        Broadcast(openedMessageType);
    }

    void Broadcast(string messageType)
    {
        try
        {
            messenger.Broadcast(messageType, EmptyContent);
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex, $"Cannot send the message {messageType}");
        }
    }
}
