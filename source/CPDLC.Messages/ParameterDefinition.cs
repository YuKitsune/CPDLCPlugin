namespace CPDLC.Messages;

/// <summary>
/// A parameter of a message element.
/// The name is the camel-case name of the type, for example "altitude".
/// If the type occurs more than one time in the element, the name has a number, for example "altitude1" and "altitude2".
/// </summary>
public record ParameterDefinition(string Name, ParameterType Type)
{
    internal static ParameterDefinition[] Create(ParameterType[] types)
    {
        var parameters = new ParameterDefinition[types.Length];
        for (var i = 0; i < types.Length; i++)
        {
            var name = char.ToLowerInvariant(types[i].ToString()[0]) + types[i].ToString().Substring(1);

            var count = types.Count(t => t == types[i]);
            if (count > 1)
            {
                var number = types.Take(i + 1).Count(t => t == types[i]);
                name += number;
            }

            parameters[i] = new ParameterDefinition(name, types[i]);
        }

        return parameters;
    }
}
