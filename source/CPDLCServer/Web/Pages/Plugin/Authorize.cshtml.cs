using CPDLCServer.Auth;
using CPDLCServer.Auth.PluginTokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Plugin;

// The browser part of the plugin login. The user logs in with VATSIM, then allows the plugin to connect.
// Then the browser goes to the plugin on this computer with a one-use code.
public class AuthorizeModel : PageModel
{
    private readonly PluginAuthorizationCodes _authorizationCodes;

    public AuthorizeModel(PluginAuthorizationCodes authorizationCodes)
    {
        _authorizationCodes = authorizationCodes;
    }

    [BindProperty(SupportsGet = true, Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [BindProperty(SupportsGet = true, Name = "state")]
    public string? State { get; set; }

    [BindProperty(SupportsGet = true, Name = "code_challenge")]
    public string? CodeChallenge { get; set; }

    [BindProperty(SupportsGet = true, Name = "code_challenge_method")]
    public string? CodeChallengeMethod { get; set; }

    public bool IsValidRequest =>
        PluginRedirectUri.IsValid(RedirectUri) &&
        !string.IsNullOrEmpty(State) &&
        !string.IsNullOrEmpty(CodeChallenge) &&
        CodeChallengeMethod == "S256";

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!IsValidRequest)
            return Page();

        var code = _authorizationCodes.Create(User.GetCid()!, User.GetName(), RedirectUri!, CodeChallenge!);

        var redirectUri = $"{RedirectUri}?code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(State!)}";
        return Redirect(redirectUri);
    }
}
