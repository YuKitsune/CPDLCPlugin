using CPDLCServer.Auth;
using CPDLCServer.Auth.Administrators;
using CPDLCServer.Auth.VatsimConnect;
using CPDLCServer.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Account;

public class LoginModel(DevelopmentLogin developmentLogin, IUnitOfWorkMediator mediator, ILogger logger) : PageModel
{
    // The page to open after the login
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(SafeReturnUrl());

        if (developmentLogin.Cid is { } cid)
            return await DevelopmentSignIn(cid, cancellationToken);

        return Page();
    }

    // A form post starts the login, so that a link on a different site cannot start it
    public IActionResult OnPost()
    {
        return Challenge(
            new AuthenticationProperties { RedirectUri = SafeReturnUrl() },
            VatsimConnectOptions.AuthenticationScheme);
    }

    async Task<IActionResult> DevelopmentSignIn(string cid, CancellationToken cancellationToken)
    {
        var identity = CurrentUser.CreateIdentity(cid, DevelopmentLogin.Name, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new(identity));

        await mediator.Send(new UpdateAdministratorNameCommand(cid, DevelopmentLogin.Name), cancellationToken);
        logger.Warning("{Cid} logged in with the development login. VATSIM Connect was not used", cid);

        return LocalRedirect(SafeReturnUrl());
    }

    // Open only pages on this server, so that a link cannot send the user to a different site
    string SafeReturnUrl() => Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/";
}
