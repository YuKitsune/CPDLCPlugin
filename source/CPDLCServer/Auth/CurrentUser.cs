using System.Security.Claims;

namespace CPDLCServer.Auth;

// Reads the VATSIM user from the claims of a logged in user
public static class CurrentUser
{
    public static int? GetCid(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var cid) ? cid : null;
    }

    public static string GetName(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    }

    public static ClaimsIdentity CreateIdentity(int cid, string name, string authenticationType)
    {
        return new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, cid.ToString()),
                new Claim(ClaimTypes.Name, name)
            ],
            authenticationType);
    }
}
