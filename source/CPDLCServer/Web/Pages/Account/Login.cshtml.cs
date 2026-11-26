using System.ComponentModel.DataAnnotations;
using CPDLCServer.Auth;
using CPDLCServer.Auth.Login;
using CPDLCServer.Auth.VatsimConnect;
using CPDLCServer.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace CPDLCServer.Web.Pages.Account;

// With VATSIM Connect, the user logs in with VATSIM. With the BuiltIn login, an administrator logs in with a password.
[EnableRateLimiting(PasswordRateLimit.PolicyName)]
public class LoginModel(AuthOptions authOptions, IUnitOfWorkMediator mediator) : PageModel
{
    public AuthOptions AuthOptions { get; } = authOptions;

    // Only for the BuiltIn login
    [BindProperty]
    public LoginInput Input { get; set; } = new();

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
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (AuthOptions.IsVatsim)
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = SafeReturnUrl() },
                VatsimConnectOptions.AuthenticationScheme);
        }

        if (!ModelState.IsValid)
            return Page();

        var result = await mediator.Send(
            new LoginCommand(Input.Cid, Input.Password, ClientIpAddress.Get(HttpContext)),
            cancellationToken);
        if (result is null)
        {
            // The same message for an unknown CID, a wrong password and a lockout
            ModelState.AddModelError(
                string.Empty,
                "The CID or password is not correct, or the login is locked because of too many failed logins. " +
                "If the login is locked, try again after 15 minutes.");
            return Page();
        }

        var identity = CurrentUser.CreateIdentity(result.Cid, result.Name, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new(identity));

        return LocalRedirect(SafeReturnUrl());
    }

    // Open only pages on this server, so that a link cannot send the user to a different site
    string SafeReturnUrl() => Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/";

    public class LoginInput
    {
        [Required(ErrorMessage = "Enter your CID.")]
        [Display(Name = "CID")]
        public string Cid { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }
}
