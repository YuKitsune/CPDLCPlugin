using System.Text.RegularExpressions;

namespace CPDLC.Messages;

/// <summary>
/// Puts parameter values into the placeholders of a template, for example "CLIMB TO AND MAINTAIN [altitude]".
/// The values replace the placeholders in sequence, from left to right.
/// </summary>
public static class ElementTemplate
{
    static readonly Regex PlaceholderRegex = new(@"\[[^\]]+\]");

    public static string Format(string template, IReadOnlyList<string> values)
    {
        var placeholders = PlaceholderRegex.Matches(template);
        if (placeholders.Count != values.Count)
        {
            throw new ArgumentException(
                $"The template \"{template}\" has {placeholders.Count} parameters, but {values.Count} values were given.",
                nameof(values));
        }

        var index = 0;
        return PlaceholderRegex.Replace(template, _ => values[index++]);
    }
}
