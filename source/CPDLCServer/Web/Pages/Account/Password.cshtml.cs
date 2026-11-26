using System.ComponentModel.DataAnnotations;
using CPDLCServer.Auth;
using CPDLCServer.Auth.Administrators;
using CPDLCServer.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Account;

// With the BuiltIn login, the administrator that is logged in changes their own password.
// With VATSIM Connect, there is no password, so the page does not exist.
public class PasswordModel(AuthOptions authOptions, IUnitOfWorkMediator mediator) : PageModel
{
    [BindProperty]
    public ChangePasswordInput Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public IActionResult OnGet()
    {
        if (!authOptions.IsBuiltIn)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!authOptions.IsBuiltIn)
            return NotFound();

        if (!ModelState.IsValid)
            return Page();

        await mediator.Send(new ChangePasswordCommand(User.GetCid()!, Input.NewPassword), cancellationToken);

        StatusMessage = "Your password was changed.";
        return RedirectToPage();
    }

    public class ChangePasswordInput
    {
        [Required(ErrorMessage = "Enter a new password.")]
        [StringLength(
            Administrator.MaximumPasswordLength,
            MinimumLength = Administrator.MinimumPasswordLength,
            ErrorMessage = "Use a password of 12 characters or more.")]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the new password again.")]
        [Compare(nameof(NewPassword), ErrorMessage = "The passwords are not the same.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
