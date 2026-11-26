using System.ComponentModel.DataAnnotations;
using CPDLCServer.Auth;
using CPDLCServer.Auth.Login;
using CPDLCServer.Auth.PluginTokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace CPDLCServer.Web.Pages.Plugin;

// The browser part of the plugin login. Then the browser goes to the plugin on this computer with a one-use code.
// - With VATSIM Connect, the user logs in with VATSIM, then allows the plugin to connect.
// - With the BuiltIn login, the plugin puts the CID of vatSys in the URL, and the page continues without a form. If the
//   URL has no CID, the user enters their CID. The server does not check the CID, so the rate limit stops a person who
//   makes many tokens.
[EnableRateLimiting(PasswordRateLimit.PolicyName)]
public class AuthorizeModel : PageModel
{
    private readonly PluginAuthorizationCodes _authorizationCodes;

    public AuthorizeModel(AuthOptions authOptions, PluginAuthorizationCodes authorizationCodes)
    {
        AuthOptions = authOptions;
        _authorizationCodes = authorizationCodes;
    }

    public AuthOptions AuthOptions { get; }

    // Only for the BuiltIn login. The plugin can put it in the URL.
    [BindProperty(SupportsGet = true, Name = "cid")]
    [Display(Name = "CID")]
    public string? Cid { get; set; }

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

    public IActionResult OnGet()
    {
        // With the BuiltIn login and a CID in the URL, the user does not need to do anything
        if (!IsValidRequest || !AuthOptions.IsBuiltIn || string.IsNullOrEmpty(Cid))
            return Page();

        return RedirectWithBuiltInCode();
    }

    public IActionResult OnPost()
    {
        if (!IsValidRequest)
            return Page();

        if (AuthOptions.IsVatsim)
            return RedirectWithCode(User.GetCid()!);

        return RedirectWithBuiltInCode();
    }

    private IActionResult RedirectWithBuiltInCode()
    {
        var cid = Cid?.Trim() ?? string.Empty;
        if (!VatsimCid.IsValid(cid))
        {
            ModelState.AddModelError(nameof(Cid), VatsimCid.ErrorMessage);
            return Page();
        }

        return RedirectWithCode(cid);
    }

    private IActionResult RedirectWithCode(string cid)
    {
        var code = _authorizationCodes.Create(cid, RedirectUri!, CodeChallenge!);

        var redirectUri = $"{RedirectUri}?code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(State!)}";
        return Redirect(redirectUri);
    }
}
