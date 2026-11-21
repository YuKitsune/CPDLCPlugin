using System.ComponentModel.DataAnnotations;
using CPDLCServer.Auth.Administrators;
using CPDLCServer.Infrastructure;
using CPDLCServer.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Setup;

// The setup adds the first administrator. After the setup, the page does not exist.
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
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Enter a valid CID.")]
    [Display(Name = "Administrator CID")]
    public int? Cid { get; set; }

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
            await _mediator.Send(new CreateFirstAdministratorCommand(Cid!.Value), cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // Another person completed the setup first
            _logger.Warning(ex, "Failed to add the first administrator");
            return NotFound();
        }

        return Redirect("/login");
    }
}
