namespace CPDLC.Messages;

/// <summary>
/// Gets the attributes of a message from the attributes of its elements.
/// </summary>
public static class MessageAttributes
{
    /// <summary>
    /// Gets the strongest response of the elements, in this sequence: W/U, A/N, R, NE.
    /// </summary>
    public static UplinkResponseAttribute ResponseAttribute(IEnumerable<UplinkMessageElement> elements)
    {
        var responseAttribute = UplinkResponseAttribute.NoResponse;
        foreach (var element in elements)
        {
            var elementResponseAttribute = UplinkElements.Get(element.Id).ResponseAttribute;
            if (Rank(elementResponseAttribute) > Rank(responseAttribute))
            {
                responseAttribute = elementResponseAttribute;
            }
        }

        return responseAttribute;
    }

    /// <summary>
    /// Gets the response of the elements. If one element needs a response, the message needs a response.
    /// </summary>
    public static DownlinkResponseAttribute ResponseAttribute(IEnumerable<DownlinkMessageElement> elements)
    {
        return elements.Any(e => DownlinkElements.Get(e.Id).ResponseAttribute == DownlinkResponseAttribute.ResponseRequired)
            ? DownlinkResponseAttribute.ResponseRequired
            : DownlinkResponseAttribute.NoResponse;
    }

    /// <summary>
    /// Gets the highest alert of the elements.
    /// </summary>
    public static AlertAttribute AlertAttribute(IEnumerable<UplinkMessageElement> elements)
    {
        return Highest(elements.Select(e => UplinkElements.Get(e.Id).AlertAttribute));
    }

    /// <summary>
    /// Gets the highest alert of the elements.
    /// </summary>
    public static AlertAttribute AlertAttribute(IEnumerable<DownlinkMessageElement> elements)
    {
        return Highest(elements.Select(e => DownlinkElements.Get(e.Id).AlertAttribute));
    }

    static AlertAttribute Highest(IEnumerable<AlertAttribute> alertAttributes)
    {
        var highest = CPDLC.Messages.AlertAttribute.None;
        foreach (var alertAttribute in alertAttributes)
        {
            if (Rank(alertAttribute) > Rank(highest))
            {
                highest = alertAttribute;
            }
        }

        return highest;
    }

    static int Rank(UplinkResponseAttribute responseAttribute)
    {
        return responseAttribute switch
        {
            UplinkResponseAttribute.WilcoUnable => 3,
            UplinkResponseAttribute.AffirmativeNegative => 2,
            UplinkResponseAttribute.Roger => 1,
            UplinkResponseAttribute.NoResponse => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(responseAttribute), responseAttribute, null)
        };
    }

    static int Rank(AlertAttribute alertAttribute)
    {
        return alertAttribute switch
        {
            CPDLC.Messages.AlertAttribute.High => 3,
            CPDLC.Messages.AlertAttribute.Medium => 2,
            CPDLC.Messages.AlertAttribute.Low => 1,
            CPDLC.Messages.AlertAttribute.None => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(alertAttribute), alertAttribute, null)
        };
    }
}
