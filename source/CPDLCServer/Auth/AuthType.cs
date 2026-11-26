namespace CPDLCServer.Auth;

public enum AuthType
{
    // Administrators log in with their CID and a password. The other pages need no login.
    // The plugin gets a token for the CID that the controller enters.
    BuiltIn,

    // All users log in with VATSIM Connect
    Vatsim
}
