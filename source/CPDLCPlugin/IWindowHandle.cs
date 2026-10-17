using System.Windows.Forms;

namespace CPDLCPlugin;

public interface IWindowHandle
{
    event EventHandler? Closed;
    event EventHandler? Moved;

    void Close();
}

public class FormWindowHandle : IWindowHandle
{
    readonly Form _form;

    public FormWindowHandle(Form form)
    {
        _form = form;
        _form.FormClosed += (_, _) => Closed?.Invoke(this, EventArgs.Empty);
        _form.Move += (_, _) => Moved?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Closed;
    public event EventHandler? Moved;

    public void Close()
    {
        _form.Close();
    }
}
