using System.Net;

namespace CPDLCServer.Auth.Login;

/// <summary>
/// Gives the IP address of the client.
/// Behind a proxy, the connection comes from the proxy, so the IP address of the client comes from a header that the proxy adds.
/// A client can also send these headers. Thus a client that connects without the proxy can choose its IP address.
/// </summary>
public static class ClientIpAddress
{
    // The first header that contains a valid IP address is used
    static readonly string[] Headers =
    [
        "do-connecting-ip",  // DigitalOcean App Platform
        "cf-connecting-ip",  // Cloudflare
        "true-client-ip",    // Akamai, Cloudflare Enterprise
        "x-real-ip",         // nginx
        "x-forwarded-for"    // Most proxies. The first address is the client.
    ];

    public static string Get(HttpContext httpContext)
    {
        foreach (var header in Headers)
        {
            if (!httpContext.Request.Headers.TryGetValue(header, out var stringValues))
                continue;

            var firstAddress = stringValues.ToString().Split(',')[0].Trim();
            if (IPAddress.TryParse(firstAddress, out var address))
                return address.ToString();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
