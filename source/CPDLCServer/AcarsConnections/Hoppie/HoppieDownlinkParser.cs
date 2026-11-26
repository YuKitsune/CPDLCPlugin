using System.Text.RegularExpressions;
using CPDLC.Messages;
using CPDLCServer.Cpdlc;

namespace CPDLCServer.AcarsConnections.Hoppie;

// Reads the text of a Hoppie CPDLC downlink as FANS 1/A downlink elements.
// Pilot clients do not all use the same words, so the parser accepts some other words and forms.
// If the parser cannot read the text, the result is one free text element (DM67) with the full text.
public static class HoppieDownlinkParser
{
    // A message has 5 elements or less
    const int MaxElements = 5;

    static readonly Regex PlaceholderRegex = new(@"\[([^\]]+)\]");
    static readonly Regex WhitespaceRegex = new(@"\s+");

    // Words in the templates that pilot clients can replace with a different word
    static readonly Dictionary<string, string> Synonyms = new()
    {
        { "DESCENT", "(?:DESCENT|DESCEND)" },
        { "DESCEND", "(?:DESCENT|DESCEND)" },
        { "ALTITUDE", "(?:ALTITUDE|LEVEL)" },
        { "SOULS", "(?:SOULS|PERSONS)" }
    };

    // Other forms of some elements, from pilot clients.
    // The parameters must have the same sequence as in the element template.
    static readonly Dictionary<int, string[]> OtherForms = new()
    {
        { 21, ["REQUEST VOICE CONTACT ON [frequency]"] },
        { 37, ["MAINTAINING [altitude]"] },
        { 57, ["[remaining fuel] FUEL REMAINING AND [remaining souls] SOULS ON BOARD"] }
    };

    // The pattern for each parameter type.
    // The types that are not in this list accept any text.
    static readonly Dictionary<ParameterType, string> ParameterPatterns = new()
    {
        { ParameterType.Altitude, @"FL\s?\d{2,3}|\d{3,5}(?:\s?(?:FT|M))?" },
        { ParameterType.Time, @"\d{1,2}:?\d{2}\s?Z?" },
        { ParameterType.Position, @"[A-Z0-9./\-]+" },
        { ParameterType.Speed, @"M\s?\.?\d{2,3}|\.\d{2,3}|\d{2,3}(?:\s?KTS?)?" },
        { ParameterType.DistanceOffset, @"\d{1,3}(?:\.\d)?\s?(?:NM|KM)?" },
        { ParameterType.Distance, @"\d{1,4}(?:\.\d)?\s?(?:NM|KM)?" },
        { ParameterType.Direction, @"LEFT|RIGHT|EITHER SIDE|NORTH EAST|NORTH WEST|SOUTH EAST|SOUTH WEST|NORTH|SOUTH|EAST|WEST" },
        { ParameterType.Degrees, @"\d{1,3}(?:\s?DEG(?:REES)?)?(?:\s?[MT])?" },
        { ParameterType.Frequency, @"\d{3}\.\d{1,3}|\d{4,5}(?:\s?KHZ)?" },
        { ParameterType.BeaconCode, @"[0-7]{4}" },
        { ParameterType.AtisCode, @"[A-Z]" },
        { ParameterType.IcaoFacilityDesignation, @"[A-Z]{4}" },
        { ParameterType.ToFrom, @"TO|FROM" },
        { ParameterType.RemainingFuel, @"\d{1,2}[:H+ ]?\d{2}|\d{1,4}" },
        { ParameterType.RemainingSouls, @"\d{1,4}" },
        { ParameterType.VersionNumber, @"\d{1,2}" }
    };

    // The parameter types in ParameterPatterns that accept almost any single word
    static readonly HashSet<ParameterType> LooseParameterTypes = [ParameterType.Position];

    // The elements that the parser does not try:
    // - the free text elements, because they are the result when nothing matches
    // - the elements that have only a parameter, because almost all text matches them
    static readonly int[] ExcludedElements = [64, 67, 68, 73];

    // The patterns are in groups with the same specificity. The most specific group is first.
    // A pattern is more specific when it has more fixed text, or, with the same fixed text, stricter parameter types.
    // The patterns without free text are in a separate list, so that a message is split into specific elements
    // before a free text parameter gets the full text.
    static readonly ElementPattern[][] SpecificPatternGroups;
    static readonly ElementPattern[][] AllPatternGroups;

    static HoppieDownlinkParser()
    {
        var patterns = DownlinkElements.All
            .Where(d => !ExcludedElements.Contains(d.Id))
            .SelectMany(d => GetTemplates(d).Select(t => CreatePattern(d, t)))
            .ToArray();

        SpecificPatternGroups = GroupBySpecificity(patterns.Where(p => !p.HasAnyTextParameter));
        AllPatternGroups = GroupBySpecificity(patterns);
    }

    static ElementPattern[][] GroupBySpecificity(IEnumerable<ElementPattern> patterns)
    {
        return patterns
            .GroupBy(p => (p.FixedTextLength, p.ParameterSpecificity))
            .OrderByDescending(g => g.Key.FixedTextLength)
            .ThenByDescending(g => g.Key.ParameterSpecificity)
            .Select(g => g.ToArray())
            .ToArray();
    }

    public static IReadOnlyList<DownlinkMessageElement> Parse(string text)
    {
        var originalText = text.Replace("@", string.Empty).Trim();
        var normalizedText = Normalize(originalText);

        var elements = ParseElements(normalizedText, SpecificPatternGroups, MaxElements)
                       ?? ParseElements(normalizedText, AllPatternGroups, MaxElements);

        return elements ?? [new DownlinkMessageElement(DownlinkElementIds.FreeText, [originalText])];
    }

    static string Normalize(string text)
    {
        var normalizedText = WhitespaceRegex.Replace(text.ToUpperInvariant(), " ");
        return normalizedText.Trim().TrimEnd('.');
    }

    // Tries the full text as one element first.
    // If that fails, tries each split at a space: the text before the split must be one element,
    // and the text after the split must be the remaining elements.
    static DownlinkMessageElement[]? ParseElements(string text, ElementPattern[][] patternGroups, int maxElements)
    {
        var element = MatchElement(text, patternGroups);
        if (element is not null)
            return [element];

        if (maxElements == 1)
            return null;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != ' ')
                continue;

            var firstText = text[..i].TrimEnd('.');
            var firstElement = MatchElement(firstText, patternGroups);
            if (firstElement is null)
                continue;

            var otherElements = ParseElements(text[(i + 1)..], patternGroups, maxElements - 1);
            if (otherElements is not null)
                return [firstElement, ..otherElements];
        }

        return null;
    }

    // Tries the most specific group first.
    // If patterns of two or more different elements in the same group match, the text is ambiguous,
    // so the parser does not use these elements, and tries the next group.
    static DownlinkMessageElement? MatchElement(string text, ElementPattern[][] patternGroups)
    {
        foreach (var patternGroup in patternGroups)
        {
            var elements = patternGroup
                .Select(pattern => Match(text, pattern))
                .OfType<DownlinkMessageElement>()
                .ToArray();

            if (elements.Length > 0 && elements.All(e => e.Id == elements[0].Id))
                return elements[0];
        }

        return null;
    }

    static DownlinkMessageElement? Match(string text, ElementPattern pattern)
    {
        var match = pattern.Regex.Match(text);
        if (!match.Success)
            return null;

        var values = new string[pattern.ParameterCount];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = match.Groups[$"p{i}"].Value.Trim();
        }

        return new DownlinkMessageElement(pattern.ElementId, values);
    }

    static IEnumerable<string> GetTemplates(DownlinkElementDefinition definition)
    {
        yield return definition.Template;

        if (OtherForms.TryGetValue(definition.Id, out var otherForms))
        {
            foreach (var otherForm in otherForms)
            {
                yield return otherForm;
            }
        }
    }

    static ElementPattern CreatePattern(DownlinkElementDefinition definition, string template)
    {
        var parts = PlaceholderRegex.Split(template);

        // Split puts the fixed text at the even indexes, and the placeholder names at the odd indexes
        var regex = new System.Text.StringBuilder("^");
        var fixedTextLength = 0;
        var parameterSpecificity = 0;
        var hasAnyTextParameter = false;
        for (var i = 0; i < parts.Length; i++)
        {
            if (i % 2 == 0)
            {
                regex.Append(FixedTextPattern(parts[i]));
                fixedTextLength += parts[i].Trim().Length;
                continue;
            }

            var parameterIndex = i / 2;
            var parameterType = definition.Parameters[parameterIndex].Type;
            if (ParameterPatterns.TryGetValue(parameterType, out var parameterPattern))
            {
                regex.Append($"(?<p{parameterIndex}>{parameterPattern})");
                parameterSpecificity += LooseParameterTypes.Contains(parameterType) ? 1 : 2;
            }
            else
            {
                regex.Append($"(?<p{parameterIndex}>.+?)");
                hasAnyTextParameter = true;
            }
        }

        regex.Append('$');

        return new ElementPattern(
            definition.Id,
            definition.Parameters.Count,
            new Regex(regex.ToString(), RegexOptions.CultureInvariant),
            fixedTextLength,
            parameterSpecificity,
            hasAnyTextParameter);
    }

    // Each space in the fixed text matches one or more spaces, and each word can be a synonym
    static string FixedTextPattern(string fixedText)
    {
        var words = fixedText.Split(' ')
            .Select(word => Synonyms.TryGetValue(word, out var synonym) ? synonym : Regex.Escape(word));

        return string.Join(@"\s+", words);
    }

    record ElementPattern(int ElementId, int ParameterCount, Regex Regex, int FixedTextLength, int ParameterSpecificity, bool HasAnyTextParameter);
}
