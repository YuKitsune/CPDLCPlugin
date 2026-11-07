using CPDLCServer.Auth.Login;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Account;

// Only a form post logs out, so that a link on a different site cannot log the user out
public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        return Redirect("/");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await AdminAuthentication.SignOut(HttpContext);
        return Redirect("/");
    }
}
