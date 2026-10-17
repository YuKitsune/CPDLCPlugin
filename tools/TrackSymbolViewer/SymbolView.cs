using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TrackSymbolViewer;

/// <summary>
/// Draws one track symbol at its origin, with a grid of 1-unit cells behind it.
/// </summary>
public class SymbolView : Control
{
    // The distance from the origin to each edge of the view, in symbol units.
    const int Extent = 16;

    // The grid shows only at this zoom or more. At a smaller zoom, the grid lines are too close.
    const double MinimumGridZoom = 4;

    static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.Parse("#2B3A44"));
    static readonly IBrush SymbolBrush = Brushes.White;
    static readonly IPen GridPen = new Pen(new SolidColorBrush(Color.Parse("#36474F")));
    static readonly IPen AxisPen = new Pen(new SolidColorBrush(Color.Parse("#5C7380")));

    public static readonly StyledProperty<TrackSymbol?> SymbolProperty =
        AvaloniaProperty.Register<SymbolView, TrackSymbol?>(nameof(Symbol));

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<SymbolView, double>(nameof(Zoom), 1);

    static SymbolView()
    {
        AffectsRender<SymbolView>(SymbolProperty, ZoomProperty);
        AffectsMeasure<SymbolView>(ZoomProperty);
    }

    public TrackSymbol? Symbol
    {
        get => GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = Extent * 2 * Zoom;
        return new Size(size, size);
    }

    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        context.FillRectangle(BackgroundBrush, new Rect(size));

        // Put the origin on a whole pixel, as vatSys does for a track position.
        var origin = new Point(Math.Round(size.Width / 2), Math.Round(size.Height / 2));

        if (Zoom >= MinimumGridZoom)
            DrawGrid(context, size, origin);

        if (Symbol?.Geometry is not { } geometry)
            return;

        var transform = Matrix.CreateScale(Zoom, Zoom) * Matrix.CreateTranslation(origin.X, origin.Y);
        using (context.PushTransform(transform))
        {
            // vatSys draws only the outline when StrokeWidth is not zero, and fills the shape when it is zero.
            if (Symbol.StrokeWidth != 0)
                context.DrawGeometry(null, new Pen(SymbolBrush, Symbol.StrokeWidth), geometry);
            else
                context.DrawGeometry(SymbolBrush, null, geometry);
        }
    }

    void DrawGrid(DrawingContext context, Size size, Point origin)
    {
        for (var unit = -Extent; unit <= Extent; unit++)
        {
            var pen = unit == 0 ? AxisPen : GridPen;

            // Add half a pixel so that each 1-pixel line is sharp.
            var x = origin.X + unit * Zoom + 0.5;
            var y = origin.Y + unit * Zoom + 0.5;
            context.DrawLine(pen, new Point(x, 0), new Point(x, size.Height));
            context.DrawLine(pen, new Point(0, y), new Point(size.Width, y));
        }
    }
}
