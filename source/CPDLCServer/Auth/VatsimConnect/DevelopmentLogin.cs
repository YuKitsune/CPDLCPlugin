namespace CPDLCServer.Auth.VatsimConnect;

// In development, the login page logs in each user with this CID and does not use VATSIM Connect.
// Set VatsimConnect__DevCid to use it. In other environments, the server ignores the value.
public record DevelopmentLogin(string? Cid)
{
    public const string Name = "Development User";
}
