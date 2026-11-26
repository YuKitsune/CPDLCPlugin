using CPDLC.Messages;

namespace CPDLCPlugin.Configuration;

// Reads the element references in the configuration file, for example "UM20" or "DM29"
public static class ElementReference
{
    public static int ParseUplink(string reference)
    {
        var id = Parse(reference, "UM");
        if (UplinkElements.Find(id) is null)
            throw new FormatException($"{reference} is not a FANS 1/A uplink element");

        return id;
    }

    public static int ParseDownlink(string reference)
    {
        var id = Parse(reference, "DM");
        if (DownlinkElements.Find(id) is null)
            throw new FormatException($"{reference} is not a FANS 1/A downlink element");

        return id;
    }

    static int Parse(string reference, string prefix)
    {
        if (!reference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(reference.Substring(prefix.Length), out var id))
        {
            throw new FormatException($"\"{reference}\" is not a valid element reference. Use the format {prefix}<number>, for example {prefix}20.");
        }

        return id;
    }
}
