using CPDLCServer.Contracts;

namespace CPDLCPlugin;

/// <summary>
/// Holds the read-only settings of the ATSU this client is connected to.
/// The settings are null when the client is not connected.
/// </summary>
public class AtsuSettingsStore
{
    public AtsuSettingsDto? Settings { get; private set; }

    public void Set(AtsuSettingsDto settings)
    {
        Settings = settings;
    }

    public void Clear()
    {
        Settings = null;
    }
}
