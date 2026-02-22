using vatsys;

namespace CPDLCPlugin;

public static class LogonCodeHelper
{
    public static async Task<string[]> TryGetLogonCode(
        int frequency,
        AtisCache atisCache,
        CancellationToken cancellationToken)
    {
        var controllers = Network.GetOnlineATCs
            .Where(c => c?.Frequencies is not null && c.Frequencies.Contains(frequency))
            .ToArray();

        var codes = new List<string>();

        foreach (var controller in controllers)
        {
            var stationCode = await TryGetLogonCode(controller, atisCache, cancellationToken);
            if (string.IsNullOrEmpty(stationCode))
                continue;

            codes.Add(stationCode);
        }

        return codes.ToArray();
    }

    public static async Task<string> TryGetLogonCode(
        string controllerCallsign,
        ControllerConnectionStore aircraftConnectionStore,
        AtisCache atisCache,
        CancellationToken cancellationToken)
    {
        // Check if this controller is connected to the CPDLC server
        var connection = await aircraftConnectionStore.Find(controllerCallsign, cancellationToken);
        if (connection is not null)
        {
            return connection.StationId;
        }

        // Fall back to ATIS query
        var controller = Network.GetOnlineATCs.FirstOrDefault(c => c.Callsign == controllerCallsign);
        if (controller is null)
            return string.Empty;

        return await TryGetLogonCode(controller, atisCache, cancellationToken);
    }

    public static async Task<string> TryGetLogonCode(
        NetworkATC controller,
        AtisCache atisCache,
        CancellationToken cancellationToken)
    {
        var atisLines = await atisCache.GetAtis(controller.Callsign, cancellationToken);
        var stationCode = TryGetLogonCode(atisLines);
        return stationCode;
    }

    static string TryGetLogonCode(string[] atisLines)
    {
        const string cpdlcPrefix = "CPDLC";
        foreach (var atisLine in atisLines)
        {
            var prefixIndex = atisLine.IndexOf(cpdlcPrefix, StringComparison.OrdinalIgnoreCase);
            if (prefixIndex < 0)
                continue;

            var afterPrefix = atisLine.Substring(prefixIndex + cpdlcPrefix.Length);
            var tokens = afterPrefix.Split([' ', '\t', ':', '='], StringSplitOptions.RemoveEmptyEntries);

            // Common filler words that controllers use in ATIS between "CPDLC" and the logon code
            // Examples:
            //  CPDLC logon is YBBB
            //  CPDLC available on YBBB
            //  CPDLC code YBBB
            //  CPDLC via YBBB
            string[] fillerWords =
            [
                "is", "available", "on", "at", "logon", "code",
                "via", "using", "with", "contact", "use", "station",
                "through"
            ];

            foreach (var token in tokens)
            {
                if (fillerWords.Any(f => f.Equals(token, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (token.Length != 4 || !token.All(char.IsLetter) || !token.All(char.IsUpper))
                    break;

                return token.ToUpperInvariant();
            }
        }

        return string.Empty;
    }
}
