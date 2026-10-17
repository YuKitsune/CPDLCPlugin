using Avalonia.Media;

namespace TrackSymbolViewer;

/// <summary>
/// One TrackSymbol element from Tracks.xml.
/// If vatSys cannot read the element, <see cref="Geometry"/> is null and <see cref="Error"/> tells why.
/// </summary>
public record TrackSymbol(
    string Type,
    string? SpecialCode,
    double StrokeWidth,
    bool RotateToTrack,
    Geometry? Geometry,
    string? Error)
{
    public string Title => SpecialCode is null ? Type : $"{Type}  (SpecialCode {SpecialCode})";

    public string Details
    {
        get
        {
            // vatSys draws only the outline when StrokeWidth is not zero, and fills the shape when it is zero.
            var details = StrokeWidth != 0 ? $"Outline, StrokeWidth {StrokeWidth}" : "Filled";
            if (RotateToTrack)
                details += ", rotates to track";

            return details;
        }
    }
}
