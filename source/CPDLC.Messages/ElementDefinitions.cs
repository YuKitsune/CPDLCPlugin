namespace CPDLC.Messages;

/// <summary>
/// The definition of a FANS 1/A uplink message element (UM).
/// </summary>
public class UplinkElementDefinition
{
    public UplinkElementDefinition(int id, string template, UplinkResponseAttribute responseAttribute, AlertAttribute alertAttribute, params ParameterType[] parameterTypes)
    {
        Id = id;
        Template = template;
        ResponseAttribute = responseAttribute;
        AlertAttribute = alertAttribute;
        Parameters = ParameterDefinition.Create(parameterTypes);
    }

    public int Id { get; }
    public string Template { get; }
    public UplinkResponseAttribute ResponseAttribute { get; }
    public AlertAttribute AlertAttribute { get; }
    public IReadOnlyList<ParameterDefinition> Parameters { get; }

    public string Format(IReadOnlyList<string> values)
    {
        return ElementTemplate.Format(Template, values);
    }
}

/// <summary>
/// The definition of a FANS 1/A downlink message element (DM).
/// </summary>
public class DownlinkElementDefinition
{
    public DownlinkElementDefinition(int id, string template, DownlinkResponseAttribute responseAttribute, AlertAttribute alertAttribute, params ParameterType[] parameterTypes)
    {
        Id = id;
        Template = template;
        ResponseAttribute = responseAttribute;
        AlertAttribute = alertAttribute;
        Parameters = ParameterDefinition.Create(parameterTypes);
    }

    public int Id { get; }
    public string Template { get; }
    public DownlinkResponseAttribute ResponseAttribute { get; }
    public AlertAttribute AlertAttribute { get; }
    public IReadOnlyList<ParameterDefinition> Parameters { get; }

    public string Format(IReadOnlyList<string> values)
    {
        return ElementTemplate.Format(Template, values);
    }
}
