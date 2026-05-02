using Serilog.Events;

namespace CPDLCPlugin.Configuration;

public class PluginConfiguration
{
    public required string ServerEndpoint { get; init; }
    public required string[] Stations { get; init; }
    public int MaxCurrentMessages { get; init; } = 50;
    public int MaxArchivedMessages { get; init; } = 50;
    public int MaxDisplayMessageLength { get; init; } = 40;
    public int MaxExtendedMessageLength { get; init; } = 80;
    public required UplinkMessagesConfiguration UplinkMessages { get; init; }
    public int MaxLogFileAgeDays { get; init; } = 5;
    public LogEventLevel LogLevel { get; init; } = LogEventLevel.Information;

    // Maps ATSU/CPDLC codes to the sector names they cover (as reported by vatSys SectorsVolumes).
    // Used to determine the Next Data Authority when walking the FDR route.
    public AtsuCodeMapping[] AtsuCodes { get; init; } = [];

    // How often (in minutes) to recalculate the NDA for all tracked aircraft.
    // NDA is also recalculated on every FDR update, so this is a catch-all interval.
    public int NdaRecalculationIntervalMinutes { get; init; } = 1;
}
