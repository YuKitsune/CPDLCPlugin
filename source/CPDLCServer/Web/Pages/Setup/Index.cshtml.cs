using System.ComponentModel.DataAnnotations;
using CPDLCServer.Infrastructure;
using CPDLCServer.Settings;
using CPDLCServer.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Setup;

// The setup adds the first administrator and the operator details. After the setup, the page does not exist.
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

    [BindProperty]
    public SetupInput Input { get; set; } = new();

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

        if (!ModelState.IsValid)
            return Page();

        try
        {
            await _mediator.Send(
                new CompleteSetupCommand(Input.Cid!.Value, Input.OperatorName, Input.PrivacyContact),
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

    public class SetupInput
    {
        [Required(ErrorMessage = "Enter a CID.")]
        [Range(1, int.MaxValue, ErrorMessage = "Enter a valid CID.")]
        [Display(Name = "Administrator CID")]
        public int? Cid { get; set; }

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
