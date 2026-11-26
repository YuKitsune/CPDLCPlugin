namespace CPDLCServer.Auth.Administrators;

// A user that can open the Configuration area of the dashboard
public class Administrator
{
    // For the BuiltIn login
    public const int MinimumPasswordLength = 12;
    public const int MaximumPasswordLength = 200;

    public required string Cid { get; init; }

    // Only for the BuiltIn login. Null with VATSIM Connect.
    public string? PasswordHash { get; set; }

    public DateTimeOffset Added { get; init; }
}
