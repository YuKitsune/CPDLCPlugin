using CPDLCServer.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Account;

// Only a form post logs out, so that a link on a different site cannot log the user out
public class LogoutModel(AuthOptions authOptions) : PageModel
{
    public IActionResult OnGet()
    {
        return Redirect("/");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // With the BuiltIn login, the pages need no login
        return Redirect(authOptions.IsBuiltIn ? "/" : "/login");
    }
}
