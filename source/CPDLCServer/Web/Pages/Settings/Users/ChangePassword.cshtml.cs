using System.ComponentModel.DataAnnotations;
using CPDLCServer.Auth.Users;
using CPDLCServer.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Settings.Users;

public class ChangePasswordModel : PageModel
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWorkMediator _mediator;

    public ChangePasswordModel(IUserRepository userRepository, IUnitOfWorkMediator mediator)
    {
        _userRepository = userRepository;
        _mediator = mediator;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public ChangePasswordInput Input { get; set; } = new();

    public string Username { get; private set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindById(Id, cancellationToken);
        if (user is null)
            return NotFound();

        Username = user.Username;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindById(Id, cancellationToken);
        if (user is null)
            return NotFound();

        Username = user.Username;
        if (!ModelState.IsValid)
            return Page();

        await _mediator.Send(new ChangePasswordCommand(Id, Input.NewPassword), cancellationToken);

        StatusMessage = $"The password of {Username} was changed.";
        return RedirectToPage("Index");
    }

    public class ChangePasswordInput
    {
        [Required]
        [StringLength(200, MinimumLength = 12, ErrorMessage = "Use a password of 12 characters or more.")]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "The passwords are not the same.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
