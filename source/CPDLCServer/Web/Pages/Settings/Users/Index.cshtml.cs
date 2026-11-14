using System.Security.Claims;
using CPDLCServer.Auth.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Settings.Users;

public class IndexModel : PageModel
{
    private readonly IUserRepository _userRepository;

    public IndexModel(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public User[] Users { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsCurrentUser(User user) => User.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id.ToString();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Users = await _userRepository.All(cancellationToken);
    }
}
