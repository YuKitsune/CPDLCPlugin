using System.Security.Claims;

namespace CPDLCServer.Auth;

// Reads the VATSIM user from the claims of a logged in user
public static class CurrentUser
{
    public static string? GetCid(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public static ClaimsIdentity CreateIdentity(string cid, string authenticationType)
    {
        return new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, cid)], authenticationType);
    }
}
