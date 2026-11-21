using CPDLCServer.Contracts;

namespace CPDLCPlugin.Server;

// The user that the plugin is authenticated as on a CPDLC server. The SETUP window shows it.
public class AuthenticationState
{
    readonly object _lock = new();
    Uri? _server;
    PluginUserDto? _user;

    public PluginUserDto? GetUser(Uri server)
    {
        lock (_lock)
        {
            return _server == server ? _user : null;
        }
    }

    public void Set(Uri server, PluginUserDto? user)
    {
        lock (_lock)
        {
            _server = server;
            _user = user;
        }
    }
}
