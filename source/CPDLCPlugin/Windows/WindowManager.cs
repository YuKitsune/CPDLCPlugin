using System.Windows;
using System.Windows.Forms.Integration;

namespace CPDLCPlugin.Windows;

public class WindowManager(IGuiInvoker guiInvoker)
{
    readonly IDictionary<string, Window> _windows = new Dictionary<string, Window>();
    readonly object _gate = new();

    public void FocusOrCreateWindow(
        string key,
        Func<IWindowHandle, Window> createWindow)
    {
        guiInvoker.InvokeOnGUI(mainForm =>
        {
            try
            {
                lock (_gate)
                {
                    if (_windows.TryGetValue(key, out var existingWindowHandle))
                    {
                        existingWindowHandle.Focus();
                        return;
                    }

                    var windowHandle = new WpfWindowHandle();
                    var window = createWindow(windowHandle);
                    windowHandle.SetWindow(window);

                    ElementHost.EnableModelessKeyboardInterop(window);

                    var helper = new System.Windows.Interop.WindowInteropHelper(window);
                    helper.Owner = mainForm.Handle;

                    window.Closed += (_, _) => TryRemoveWindowInternal(key);
                    window.Show();

                    _windows[key] = window;
                }
            }
            catch (Exception ex)
            {
                Plugin.AddError(ex, "Failed to open window");
            }
        });
    }

    public void TryRemoveWindow(string key)
    {
        guiInvoker.InvokeOnGUI(_ =>
        {
            lock (_gate)
            {
                if (!_windows.TryGetValue(key, out var window))
                    return;

                try
                {
                    window.Close();
                }
                catch (InvalidOperationException)
                {
                    // Window may have already been closed or disposed
                }
            }
        });
    }

    void TryRemoveWindowInternal(string key)
    {
        lock (_gate)
        {
            _windows.Remove(key);
        }
    }
}
