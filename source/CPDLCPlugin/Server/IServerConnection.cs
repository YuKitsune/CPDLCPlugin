namespace CPDLCPlugin.Server;

/// <summary>
/// The connection to the CPDLC server, without a dependency on the vatSys <see cref="Plugin"/>.
/// </summary>
public interface IServerConnection
{
    bool IsConnected { get; }
    string StationIdentifier { get; }

    Task UpdateNextDataAuthority(
        string callsign,
        string? nextDataAuthority,
        DateTimeOffset? expectedTransferTime,
        CancellationToken cancellationToken);
}

public class PluginServerConnection(Plugin plugin) : IServerConnection
{
    public bool IsConnected => plugin.ConnectionManager is { IsConnected: true };

    public string StationIdentifier => plugin.ConnectionManager?.StationIdentifier ?? string.Empty;

    public async Task UpdateNextDataAuthority(
        string callsign,
        string? nextDataAuthority,
        DateTimeOffset? expectedTransferTime,
        CancellationToken cancellationToken)
    {
        if (plugin.ConnectionManager is null)
            throw new InvalidOperationException("Not connected to the server");

        await plugin.ConnectionManager.UpdateNextDataAuthority(
            callsign,
            nextDataAuthority,
            expectedTransferTime,
            cancellationToken);
    }
}
