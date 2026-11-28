namespace CPDLC.Messages;

/// <summary>
/// Writes the elements of a message as text, for example "UNABLE. DUE TO TRAFFIC".
/// </summary>
public static class MessageText
{
    public static string Format(IEnumerable<UplinkMessageElement> elements)
    {
        return string.Join(". ", elements.Select(e => UplinkElements.Get(e.Id).Format(e.Parameters)));
    }

    public static string Format(IEnumerable<DownlinkMessageElement> elements)
    {
        return string.Join(". ", elements.Select(e => DownlinkElements.Get(e.Id).Format(e.Parameters)));
    }
}
