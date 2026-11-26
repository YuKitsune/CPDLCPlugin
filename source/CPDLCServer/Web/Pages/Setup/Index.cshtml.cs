using System.ComponentModel.DataAnnotations;
using CPDLCServer.Auth;
using CPDLCServer.Auth.Administrators;
using CPDLCServer.Infrastructure;
using CPDLCServer.Settings;
using CPDLCServer.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Setup;

// The setup adds the first administrator and the operator details. After the setup, the page does not exist.
// With the BuiltIn login, the page needs the token from the server log.
public class IndexModel : PageModel
{
    private readonly SetupState _setupState;
    private readonly IUnitOfWorkMediator _mediator;
    private readonly ILogger _logger;

    public IndexModel(
        AuthOptions authOptions,
        SetupState setupState,
        IUnitOfWorkMediator mediator,
        ILogger logger)
    {
        AuthOptions = authOptions;
        _setupState = setupState;
        _mediator = mediator;
        _logger = logger;
    }

    public AuthOptions AuthOptions { get; }

    // The token from the setup URL in the server log. Only for the BuiltIn login.
    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    [BindProperty]
    public SetupInput Input { get; set; } = new();

    public bool IsTokenValid => AuthOptions.IsVatsim || _setupState.IsValidSetupToken(Token);

    public IActionResult OnGet()
    {
        if (_setupState.IsConfigured)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (_setupState.IsConfigured)
            return NotFound();

        if (!IsTokenValid)
            return Page();

        if (AuthOptions.IsBuiltIn)
            ValidateBuiltInAdministrator();

        if (!ModelState.IsValid)
            return Page();

        try
        {
            await _mediator.Send(
                new CompleteSetupCommand(
                    Input.Cid!,
                    AuthOptions.IsBuiltIn ? Input.Password : null,
                    Input.OperatorName,
                    Input.PrivacyContact),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // Another person completed the setup first
            _logger.Warning(ex, "Failed to complete the setup");
            return NotFound();
        }

        return Redirect("/login");
    }

    // The password is necessary only for the BuiltIn login, so it does not have validation attributes
    void ValidateBuiltInAdministrator()
    {
        if (Input.Password is not { Length: >= Administrator.MinimumPasswordLength and <= Administrator.MaximumPasswordLength })
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Password)}", "Use a password of 12 characters or more.");
        else if (Input.Password != Input.ConfirmPassword)
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.ConfirmPassword)}", "The passwords are not the same.");
    }

    public class SetupInput
    {
        [Required(ErrorMessage = "Enter a CID.")]
        [StringLength(VatsimCid.MaxLength)]
        [RegularExpression(VatsimCid.Pattern, ErrorMessage = VatsimCid.ErrorMessage)]
        [Display(Name = "Administrator CID")]
        public string? Cid { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Enter the operator name.")]
        [StringLength(ServerSettings.MaxOperatorNameLength)]
        [Display(Name = "Operator name")]
        public string OperatorName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a privacy contact.")]
        [StringLength(ServerSettings.MaxPrivacyContactLength)]
        [PrivacyContact]
        [Display(Name = "Privacy contact")]
        public string PrivacyContact { get; set; } = string.Empty;
    }
}
