using System.Windows;
using System.Windows.Forms;

namespace CPDLCPlugin.Windows;

public class WindowManager(IGuiInvoker guiInvoker)
{
    readonly IDictionary<string, VatSysForm> _forms = new Dictionary<string, VatSysForm>();
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

                    configureForm?.Invoke(form);

                    form.Closed += (_, _) => TryRemoveWindowInternal(key);
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
