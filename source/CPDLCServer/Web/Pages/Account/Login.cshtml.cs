using CPDLCServer.Auth.VatsimConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Account;

public class LoginModel : PageModel
{
    // The page to open after the login
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(SafeReturnUrl());

        return Page();
    }

    // A form post starts the login, so that a link on a different site cannot start it
    public IActionResult OnPost()
    {
        return Challenge(
            new AuthenticationProperties { RedirectUri = SafeReturnUrl() },
            VatsimConnectOptions.AuthenticationScheme);
    }

    // Open only pages on this server, so that a link cannot send the user to a different site
    string SafeReturnUrl() => Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/";
}
