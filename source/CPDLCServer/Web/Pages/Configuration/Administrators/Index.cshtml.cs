using System.ComponentModel.DataAnnotations;
using CPDLCServer.Auth;
using CPDLCServer.Auth.Administrators;
using CPDLCServer.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Configuration.Administrators;

public class IndexModel : PageModel
{
    private readonly IAdministratorRepository _administratorRepository;
    private readonly IUnitOfWorkMediator _mediator;

    public IndexModel(AuthOptions authOptions, IAdministratorRepository administratorRepository, IUnitOfWorkMediator mediator)
    {
        AuthOptions = authOptions;
        _administratorRepository = administratorRepository;
        _mediator = mediator;
    }

    public AuthOptions AuthOptions { get; }

    public Administrator[] Administrators { get; private set; } = [];

    [BindProperty]
    [Required(ErrorMessage = "Enter a CID.")]
    [StringLength(VatsimCid.MaxLength)]
    [RegularExpression(VatsimCid.Pattern, ErrorMessage = VatsimCid.ErrorMessage)]
    [Display(Name = "CID")]
    public string? Cid { get; set; }

    // The name and the password are only for the BuiltIn login, so they do not have validation attributes
    [BindProperty]
    [StringLength(64)]
    [Display(Name = "Name")]
    public string? Name { get; set; }

    [BindProperty]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    [BindProperty]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string? ConfirmPassword { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public bool IsCurrentUser(Administrator administrator) => User.GetCid() == administrator.Cid;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Administrators = await _administratorRepository.All(cancellationToken);
    }

    public async Task<IActionResult> OnPostAddAsync(CancellationToken cancellationToken)
    {
        if (AuthOptions.IsBuiltIn)
            ValidateBuiltInAdministrator();

        if (!ModelState.IsValid)
        {
            Administrators = await _administratorRepository.All(cancellationToken);
            return Page();
        }

        try
        {
            await _mediator.Send(
                new AddAdministratorCommand(
                    Cid!,
                    AuthOptions.IsBuiltIn ? Name : null,
                    AuthOptions.IsBuiltIn ? Password : null),
                cancellationToken);
            StatusMessage = $"{Cid} is now an administrator.";
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(string cid, CancellationToken cancellationToken)
    {
        var currentUserCid = User.GetCid();
        if (currentUserCid is null)
            return Forbid();

        try
        {
            await _mediator.Send(new RemoveAdministratorCommand(cid, currentUserCid), cancellationToken);
            StatusMessage = $"{cid} is no longer an administrator.";
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    void ValidateBuiltInAdministrator()
    {
        if (string.IsNullOrWhiteSpace(Name))
            ModelState.AddModelError(nameof(Name), "Enter the name of the administrator.");

        if (Password is not { Length: >= Administrator.MinimumPasswordLength and <= Administrator.MaximumPasswordLength })
            ModelState.AddModelError(nameof(Password), "Use a password of 12 characters or more.");
        else if (Password != ConfirmPassword)
            ModelState.AddModelError(nameof(ConfirmPassword), "The passwords are not the same.");
    }
}
