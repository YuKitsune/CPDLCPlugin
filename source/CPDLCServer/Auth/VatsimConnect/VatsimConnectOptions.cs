namespace CPDLCServer.Auth.VatsimConnect;

// The OAuth client of this server on VATSIM Connect. Make the client at https://auth.vatsim.net (or https://auth-dev.vatsim.net).
// The redirect URL of the client is https://<server>/signin-vatsim
public class VatsimConnectOptions
{
    public const string AuthenticationScheme = "VatsimConnect";
    public const string CallbackPath = "/signin-vatsim";

    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public Uri Authority { get; init; } = new("https://auth.vatsim.net");
}
