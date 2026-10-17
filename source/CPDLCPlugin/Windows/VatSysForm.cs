using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using vatsys;

namespace CPDLCPlugin.Windows;

// A vatSys form that hosts WPF content
public class VatSysForm : BaseForm
{
    public VatSysForm(string title, Func<IWindowHandle, FrameworkElement> createView, bool sizeToContent)
    {
        MiddleClickClose = false;

        Text = title;

        var view = createView(new FormWindowHandle(this));

        var elementHost = new ElementHost
        {
            Dock = DockStyle.Fill
        };

        if (sizeToContent)
        {
            // A Canvas measures its child with no size limit. The view then always has its natural size,
            // and SizeChanged occurs each time the content changes size.
            var canvas = new Canvas();
            canvas.Children.Add(view);
            elementHost.Child = canvas;

            view.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            ResizeToContent(view.DesiredSize);
            view.SizeChanged += (_, e) => ResizeToContent(e.NewSize);

            FormBorderStyle = FormBorderStyle.FixedSingle;
        }
        else
        {
            elementHost.Child = view;

            FormBorderStyle = FormBorderStyle.Sizable;
            Resizeable = true;
        }

        Controls.Add(elementHost);

        Padding = new Padding(0);
        Margin = new Padding(0);
    }

    void ResizeToContent(Size contentSize)
    {
        if (WindowState != FormWindowState.Normal)
            return;

        // vatSys draws its own border and title bar. The ClientSize setter uses the standard Windows border to
        // calculate the form size, so it can give the wrong size. Use the real size of the border and title bar.
        var borderWidth = Width - ClientSize.Width;
        var borderHeight = Height - ClientSize.Height;

        Size = new System.Drawing.Size(
            (int)Math.Ceiling(contentSize.Width) + borderWidth,
            (int)Math.Ceiling(contentSize.Height) + borderHeight);
    }
}
