namespace CPDLCPlugin.Server;

public static class ServerAddress
{
    // The configuration has the URL of the hub, for example https://cpdlc.example.com/hubs/controller.
    // The authentication endpoints are at the root of the same server.
    public static Uri GetBaseUri(string serverEndpoint)
    {
        if (!Uri.TryCreate(serverEndpoint, UriKind.Absolute, out var endpoint))
            throw new ArgumentException($"The server URL {serverEndpoint} is not valid", nameof(serverEndpoint));

        return new Uri(endpoint.GetLeftPart(UriPartial.Authority) + "/");
    }
}
