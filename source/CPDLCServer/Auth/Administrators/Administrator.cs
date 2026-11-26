namespace CPDLCServer.Auth.Administrators;

// A user that can open the Configuration area of the dashboard
public class Administrator
{
    // For the BuiltIn login
    public const int MinimumPasswordLength = 12;
    public const int MaximumPasswordLength = 200;

    public required string Cid { get; init; }

    // With VATSIM Connect, the VATSIM API does not give the name for a CID, so the name is not known until the
    // administrator logs in. With the BuiltIn login, the name is set when the administrator is added.
    public string? Name { get; set; }

    // Only for the BuiltIn login. Null with VATSIM Connect.
    public string? PasswordHash { get; set; }

    public DateTimeOffset Added { get; init; }
}
