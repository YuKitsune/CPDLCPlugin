using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using vatsys;

namespace CPDLCPlugin.Windows;

// A vatSys form that hosts WPF content
public class VatSysForm : BaseForm
{
    public VatSysForm(string title, Func<IWindowHandle, UIElement> createView, bool shrinkToContent)
    {
        MiddleClickClose = false;

        Text = title;

        var child = createView(new FormWindowHandle(this));

        var elementHost = new ElementHost();
        if (shrinkToContent)
        {
            // Measure with a maximum width so that text can wrap
            const int maxWidth = 1000;
            child.Measure(new Size(maxWidth, double.PositiveInfinity));
            child.Arrange(new Rect(child.DesiredSize));
            child.UpdateLayout();

            var desired = child.DesiredSize;
            elementHost.Child = child;
            elementHost.Size = new System.Drawing.Size((int)Math.Ceiling(desired.Width), (int)Math.Ceiling(desired.Height));
            elementHost.Location = new System.Drawing.Point(0, 0);

            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = elementHost.Size;
        }
        else
        {
            elementHost.Child = child;
            elementHost.Dock = DockStyle.Fill;
        }

        Controls.Add(elementHost);

        Padding = new Padding(0);
        Margin = new Padding(0);
        PerformLayout();
    }
}
