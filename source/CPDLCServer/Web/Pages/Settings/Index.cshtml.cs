using CPDLCServer.Infrastructure;
using CPDLCServer.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Settings;

public class IndexModel : PageModel
{
    private readonly IServerSettingsProvider _settingsProvider;
    private readonly IUnitOfWorkMediator _mediator;

    public IndexModel(IServerSettingsProvider settingsProvider, IUnitOfWorkMediator mediator)
    {
        _settingsProvider = settingsProvider;
        _mediator = mediator;
    }

    [BindProperty]
    public ServerSettings Settings { get; set; } = new();

    public ServerSettings Defaults { get; } = new();

    [TempData]
    public bool Saved { get; set; }

    // If one connectivity check fails, the time between two updates of the last seen time is two check intervals
    public bool OneFailedCheckCausesTimeout =>
        Settings.AircraftConnectionTimeoutMinutes <= Settings.ConnectivityCheckIntervalMinutes * 2;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Settings = await _settingsProvider.Load(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (Settings.VatsimStatusUrl is { IsAbsoluteUri: false })
            ModelState.AddModelError($"{nameof(Settings)}.{nameof(Settings.VatsimStatusUrl)}", "Enter an absolute URL.");

        if (!ModelState.IsValid)
            return Page();

        await _mediator.Send(new SaveServerSettingsCommand(Settings), cancellationToken);

        Saved = true;
        return RedirectToPage();
    }
}
