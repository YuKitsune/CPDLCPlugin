namespace CPDLCServer.Auth.Administrators;

// A VATSIM user that can open the Configuration area of the dashboard
public class Administrator
{
    public int Cid { get; init; }

    // The VATSIM API does not give the name for a CID, so the name is not known until the administrator logs in
    public string? Name { get; set; }

    public DateTimeOffset Added { get; init; }
}
