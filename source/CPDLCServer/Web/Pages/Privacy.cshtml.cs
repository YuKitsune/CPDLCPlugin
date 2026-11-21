using CPDLCServer.AcarsConnections.Traffic;
using CPDLCServer.AdscContracts;
using CPDLCServer.AdscContracts.MessageLog;
using CPDLCServer.Auth;
using CPDLCServer.Auth.PluginTokens;
using CPDLCServer.Logging;
using CPDLCServer.Settings;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages;

// The privacy policy. The values come from the settings and the code, so the page is correct when they change.
public class PrivacyModel : PageModel
{
    private readonly IServerSettingsProvider _settingsProvider;
    private readonly LogBuffer _logBuffer;
    private readonly AcarsTrafficLog _trafficLog;

    public PrivacyModel(IServerSettingsProvider settingsProvider, LogBuffer logBuffer, AcarsTrafficLog trafficLog)
    {
        _settingsProvider = settingsProvider;
        _logBuffer = logBuffer;
        _trafficLog = trafficLog;
    }

    public ServerSettings Settings { get; private set; } = new();

    public TimeSpan SessionLifetime => AuthServiceCollectionExtensions.SessionLifetime;
    public TimeSpan PluginTokenLifetime => PluginToken.Lifetime;
    public TimeSpan AdscMessageLogRetention => PurgeExpiredAdscDataRequestHandler.MessageLogRetention;
    public int AdscMessageLogCapacity => InMemoryAdscMessageLogRepository.Capacity;
    public int LogCapacity => _logBuffer.Capacity;
    public int TrafficLogCapacity => _trafficLog.Capacity;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Settings = await _settingsProvider.Load(cancellationToken);
    }
}
