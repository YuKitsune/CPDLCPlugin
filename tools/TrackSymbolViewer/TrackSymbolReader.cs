using System.Globalization;
using System.Xml;
using Avalonia;
using Avalonia.Media;

namespace TrackSymbolViewer;

/// <summary>
/// Reads Tracks.xml with the same rules as vatSys (vatsys.TrackSymbols.LoadSymbol).
/// If vatSys cannot read a symbol, the symbol gets an error message.
/// The reader accepts all Type values, so that you can design symbols for types that vatSys does not know.
/// </summary>
public static class TrackSymbolReader
{
    const double DefaultStrokeWidth = 2;

    // vatSys rotates these types to the aircraft heading when RotateToTrack is not set.
    static readonly string[] GroundTypes = ["Ground", "GroundLight", "GroundMedium", "GroundHeavy", "GroundSuper"];

    public static List<TrackSymbol> Read(string path)
    {
        var document = new XmlDocument();
        document.Load(path);

        var root = document["TrackSymbols"]
            ?? throw new InvalidDataException("The file does not have a TrackSymbols root element.");

        var symbols = new List<TrackSymbol>();
        foreach (XmlElement element in root.GetElementsByTagName("TrackSymbol"))
            symbols.Add(ReadSymbol(element));

        return symbols;
    }

    static TrackSymbol ReadSymbol(XmlElement element)
    {
        var type = element.GetAttribute("Type");
        var specialCode = element.HasAttribute("SpecialCode") ? element.GetAttribute("SpecialCode") : null;

        try
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new FormatException("The Type attribute is missing.");

            // vatSys reads the special code as an octal number.
            if (specialCode is not null && !IsOctal(specialCode))
                throw new FormatException($"SpecialCode \"{specialCode}\" is not a valid octal code.");

            var strokeWidth = element.HasAttribute("StrokeWidth")
                ? ParseNumber(element.GetAttribute("StrokeWidth"), "StrokeWidth")
                : DefaultStrokeWidth;

            var rotateToTrack = ReadRotateToTrack(element, type);
            var geometry = ReadGeometry(element);

            return new TrackSymbol(type, specialCode, strokeWidth, rotateToTrack, geometry, null);
        }
        catch (FormatException exception)
        {
            return new TrackSymbol(type, specialCode, DefaultStrokeWidth, false, null, exception.Message);
        }
    }

    static bool ReadRotateToTrack(XmlElement element, string type)
    {
        if (!element.HasAttribute("RotateToTrack"))
            return GroundTypes.Contains(type);

        return element.GetAttribute("RotateToTrack") switch
        {
            "Default" => GroundTypes.Contains(type),
            "True" => true,
            "False" => false,
            var value => throw new FormatException($"RotateToTrack \"{value}\" must be Default, True, or False.")
        };
    }

    static Geometry ReadGeometry(XmlElement symbolElement)
    {
        // vatSys reads the first child node. A comment before the geometry element causes an error in vatSys.
        if (symbolElement.FirstChild is not XmlElement element)
            throw new FormatException("The first child node of TrackSymbol must be the geometry element.");

        return element.Name switch
        {
            "RectangleGeometry" => new RectangleGeometry(new Rect(
                ParsePoint(element.GetAttribute("TopLeft"), "TopLeft"),
                ParsePoint(element.GetAttribute("BottomRight"), "BottomRight"))),
            "EllipseGeometry" => new EllipseGeometry
            {
                Center = ParsePoint(element.GetAttribute("Center"), "Center"),
                RadiusX = ParseNumber(element.GetAttribute("RadiusX"), "RadiusX"),
                RadiusY = ParseNumber(element.GetAttribute("RadiusY"), "RadiusY")
            },
            "PathGeometry" => ReadPathGeometry(element),
            _ => throw new FormatException($"\"{element.Name}\" is not a valid geometry element.")
        };
    }

    static PathGeometry ReadPathGeometry(XmlElement pathElement)
    {
        var geometry = new PathGeometry { Figures = new PathFigures() };

        foreach (XmlElement figureElement in pathElement.GetElementsByTagName("Figure"))
        {
            var figure = new PathFigure
            {
                StartPoint = ParsePoint(figureElement.GetAttribute("Start"), "Start"),
                IsFilled = figureElement.GetAttribute("FigureBegin") switch
                {
                    "Filled" => true,
                    "Hollow" => false,
                    var value => throw new FormatException($"FigureBegin \"{value}\" must be Filled or Hollow.")
                },
                IsClosed = figureElement.GetAttribute("FigureEnd") switch
                {
                    "Closed" => true,
                    "Open" => false,
                    var value => throw new FormatException($"FigureEnd \"{value}\" must be Open or Closed.")
                },
                Segments = new PathSegments()
            };

            foreach (XmlNode node in figureElement.ChildNodes)
            {
                // vatSys casts each child node of a Figure to an element. A comment causes an error in vatSys.
                if (node is not XmlElement segmentElement)
                    throw new FormatException("A Figure must contain only Line and Bezier elements. Remove the comments.");

                switch (segmentElement.Name)
                {
                    case "Line":
                        figure.Segments.Add(new LineSegment { Point = ParsePoint(segmentElement.InnerText, "Line") });
                        break;

                    case "Bezier":
                        figure.Segments.Add(new BezierSegment
                        {
                            Point1 = ParsePoint(segmentElement["Point1"]?.InnerText, "Bezier Point1"),
                            Point2 = ParsePoint(segmentElement["Point2"]?.InnerText, "Bezier Point2"),
                            Point3 = ParsePoint(segmentElement["Point3"]?.InnerText, "Bezier Point3")
                        });
                        break;

                    // vatSys ignores other elements.
                }
            }

            geometry.Figures.Add(figure);
        }

        return geometry;
    }

    static Point ParsePoint(string? text, string name)
    {
        var parts = text?.Split(',');
        if (parts is not { Length: 2 })
            throw new FormatException($"{name} \"{text}\" must be a point in the format \"x,y\".");

        return new Point(ParseNumber(parts[0], name), ParseNumber(parts[1], name));
    }

    static double ParseNumber(string text, string name)
    {
        if (!double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw new FormatException($"{name} \"{text}\" is not a valid number.");

        return value;
    }

    static bool IsOctal(string text)
    {
        try
        {
            Convert.ToInt16(text, 8);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or OverflowException)
        {
            return false;
        }
    }
}
