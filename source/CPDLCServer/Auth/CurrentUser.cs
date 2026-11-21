using System.Security.Claims;

namespace CPDLCServer.Auth;

// Reads the VATSIM user from the claims of a logged in user
public static class CurrentUser
{
    public static string? GetCid(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public static string GetName(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    }

    public static ClaimsIdentity CreateIdentity(string cid, string name, string authenticationType)
    {
        return new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, cid),
                new Claim(ClaimTypes.Name, name)
            ],
            authenticationType);
    }
}
