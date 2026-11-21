namespace CPDLCServer.Auth.PluginTokens;

// The plugin gets the code on a port of the computer that it runs on. Only these addresses are accepted, so that a
// link cannot send the code to a different computer.
public static class PluginRedirectUri
{
    public static bool IsValid(string? redirectUri)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttp &&
               uri.Host is "127.0.0.1" or "localhost" &&
               !uri.IsDefaultPort &&
               string.IsNullOrEmpty(uri.Query) &&
               string.IsNullOrEmpty(uri.Fragment) &&
               string.IsNullOrEmpty(uri.UserInfo);
    }
}
