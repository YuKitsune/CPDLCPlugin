namespace CPDLCServer.Auth.Users;

// A user that can open the restricted pages of the dashboard
public class User
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset Created { get; init; }
}
