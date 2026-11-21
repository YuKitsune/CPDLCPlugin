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

    public IndexModel(IAdministratorRepository administratorRepository, IUnitOfWorkMediator mediator)
    {
        _administratorRepository = administratorRepository;
        _mediator = mediator;
    }

    public Administrator[] Administrators { get; private set; } = [];

    [BindProperty]
    [Required(ErrorMessage = "Enter a CID.")]
    [Range(1, int.MaxValue, ErrorMessage = "Enter a valid CID.")]
    [Display(Name = "CID")]
    public int? Cid { get; set; }

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
        if (!ModelState.IsValid)
        {
            Administrators = await _administratorRepository.All(cancellationToken);
            return Page();
        }

        try
        {
            await _mediator.Send(new AddAdministratorCommand(Cid!.Value), cancellationToken);
            StatusMessage = $"{Cid} is now an administrator.";
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(int cid, CancellationToken cancellationToken)
    {
        var currentUserCid = User.GetCid();
        if (currentUserCid is null)
            return Forbid();

        try
        {
            await _mediator.Send(new RemoveAdministratorCommand(cid, currentUserCid.Value), cancellationToken);
            StatusMessage = $"{cid} is no longer an administrator.";
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
