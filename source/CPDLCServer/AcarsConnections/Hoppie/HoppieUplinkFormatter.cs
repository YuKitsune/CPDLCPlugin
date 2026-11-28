using CPDLC.Messages;
using CPDLCServer.Cpdlc;

namespace CPDLCServer.AcarsConnections.Hoppie;

// Writes uplink elements as the text of a Hoppie CPDLC packet.
// Each parameter value is between "@" characters, so that the aircraft can show the values.
public static class HoppieUplinkFormatter
{
    public static string Format(IReadOnlyList<UplinkMessageElement> elements)
    {
        return string.Join(". ", elements.Select(FormatElement));
    }

    static string FormatElement(UplinkMessageElement element)
    {
        // Hoppie has no END SERVICE. The aircraft ends the connection when it receives LOGOFF.
        if (element.Id == UplinkElementIds.EndService)
            return "LOGOFF";

        // An "@" in a value would end the value too early, so remove it
        var values = element.Parameters
            .Select(value => $"@{value.Replace("@", string.Empty)}@")
            .ToArray();

        return UplinkElements.Get(element.Id).Format(values);
    }
}
