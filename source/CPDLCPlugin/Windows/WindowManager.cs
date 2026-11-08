using System.Windows;
using System.Windows.Forms;

namespace CPDLCPlugin.Windows;

public class WindowManager(IGuiInvoker guiInvoker)
{
    readonly IDictionary<string, VatSysForm> _forms = new Dictionary<string, VatSysForm>();
    readonly IDictionary<string, System.Drawing.Point> _lastLocations = new Dictionary<string, System.Drawing.Point>();
    readonly object _gate = new();

    public void FocusOrCreateWindow(
        string key,
        string title,
        Func<IWindowHandle, FrameworkElement> createView,
        bool sizeToContent = true,
        Action<VatSysForm>? configureForm = null)
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

                    var form = new VatSysForm(title, createView, sizeToContent)
                    {
                        StartPosition = FormStartPosition.CenterParent
                    };

                    if (_lastLocations.TryGetValue(key, out var lastLocation))
                    {
                        form.StartPosition = FormStartPosition.Manual;
                        form.Location = lastLocation;
                    }

                    configureForm?.Invoke(form);

                    form.Closed += (_, _) =>
                    {
                        // If the window is minimised or maximised, Location is not the normal position of the window
                        _lastLocations[key] = form.WindowState == FormWindowState.Normal
                            ? form.Location
                            : form.RestoreBounds.Location;

                        TryRemoveWindowInternal(key);
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

    public void TryRemoveWindow(string key)
    {
        guiInvoker.InvokeOnGUI(_ =>
        {
            lock (_gate)
            {
                if (!_forms.TryGetValue(key, out var form))
                    return;

                try
                {
                    form.Close();
                }
                catch (ObjectDisposedException)
                {
                    // Form may have already been closed or disposed
                }
            }
        });
    }

    void TryRemoveWindowInternal(string key)
    {
        lock (_gate)
        {
            _forms.Remove(key);
        }
    }
}
