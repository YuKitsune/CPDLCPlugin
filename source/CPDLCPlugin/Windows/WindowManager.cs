using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace CPDLCPlugin.Windows;

public class WindowManager(IGuiInvoker guiInvoker)
{
    readonly IDictionary<string, Window> _windows = new Dictionary<string, Window>();
    readonly IDictionary<string, VatSysForm> _forms = new Dictionary<string, VatSysForm>();
    readonly object _gate = new();

    public void FocusOrCreateForm(
        string key,
        string title,
        Func<IWindowHandle, UIElement> createView,
        Action? onClosed = null)
    {
        guiInvoker.InvokeOnGUI(mainForm =>
        {
            try
            {
                lock (_gate)
                {
                    if (_forms.TryGetValue(key, out var existingForm))
                    {
                        existingForm.WindowState = FormWindowState.Normal;
                        existingForm.Activate();
                        return;
                    }

                    var form = new VatSysForm(title, createView, shrinkToContent: true)
                    {
                        StartPosition = FormStartPosition.CenterParent
                    };

                    form.Closed += (_, _) =>
                    {
                        TryRemoveFormInternal(key);
                        onClosed?.Invoke();
                    };
                    form.Show(mainForm);

                    _forms[key] = form;
                }
            }
            catch (Exception ex)
            {
                Plugin.AddError(ex, "Failed to open window");
            }
        });
    }

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
                if (_forms.TryGetValue(key, out var form))
                {
                    form.Close();
                    return;
                }

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

    void TryRemoveFormInternal(string key)
    {
        lock (_gate)
        {
            _forms.Remove(key);
        }
    }
}
