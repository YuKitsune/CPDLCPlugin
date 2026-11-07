using System.ComponentModel.DataAnnotations;
using CPDLCServer.Auth.Login;
using CPDLCServer.Auth.Users;
using CPDLCServer.Infrastructure;
using CPDLCServer.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Setup;

// The setup: create the first user
public class IndexModel : PageModel
{
    private readonly SetupState _setupState;
    private readonly IUnitOfWorkMediator _mediator;
    private readonly ILogger _logger;

    public IndexModel(
        SetupState setupState,
        IUnitOfWorkMediator mediator,
        ILogger logger)
    {
        _setupState = setupState;
        _mediator = mediator;
        _logger = logger;
    }

    // The token from the setup URL in the server log
    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    [BindProperty]
    public CreateAdminInput Input { get; set; } = new();

    public bool IsTokenValid { get; private set; }

    public IActionResult OnGet()
    {
        if (_setupState.IsConfigured)
            return Redirect("/");

        IsTokenValid = _setupState.IsValidSetupToken(Token);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (_setupState.IsConfigured)
            return Redirect("/");

        IsTokenValid = _setupState.IsValidSetupToken(Token);
        if (!IsTokenValid)
            return Page();

        if (!ModelState.IsValid)
            return Page();

        Guid userId;
        try
        {
            userId = await _mediator.Send(new CreateUserCommand(Input.Username, Input.Password), cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // Another person completed this step first
            _logger.Warning(ex, "Failed to create the administrator");
            return RedirectToPage("/Account/Login");
        }

        await AdminAuthentication.SignIn(HttpContext, userId, Input.Username.Trim());

        return Redirect("/");
    }

    public class CreateAdminInput
    {
        [Required]
        [StringLength(64)]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 12, ErrorMessage = "Use a password of 12 characters or more.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password), ErrorMessage = "The passwords are not the same.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
