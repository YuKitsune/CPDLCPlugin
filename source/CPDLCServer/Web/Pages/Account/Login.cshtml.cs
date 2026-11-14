using System.ComponentModel.DataAnnotations;
using CPDLCServer.Auth.Login;
using CPDLCServer.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace CPDLCServer.Web.Pages.Account;

[EnableRateLimiting(PasswordRateLimit.PolicyName)]
public class LoginModel : PageModel
{
    private readonly IUnitOfWorkMediator _mediator;

    public LoginModel(IUnitOfWorkMediator mediator)
    {
        _mediator = mediator;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    // The page to open after the login
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        var result = await _mediator.Send(new LoginCommand(Input.Username, Input.Password, ClientIpAddress.Get(HttpContext)), cancellationToken);
        if (result is null)
        {
            // The same message for an unknown user, a wrong password and a lockout
            ModelState.AddModelError(
                string.Empty,
                "The username or password is not correct, or the login is locked because of too many failed logins. " +
                "If the login is locked, try again after 15 minutes.");
            return Page();
        }

        await AdminAuthentication.SignIn(HttpContext, result.UserId, result.Username);

        // Open only pages on this server, so that a link cannot send the user to a different site
        return Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl) : Redirect("/");
    }

    public class LoginInput
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }
}
