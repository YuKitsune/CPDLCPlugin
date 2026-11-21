using CPDLCServer.Infrastructure;
using CPDLCServer.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Configuration.Settings;

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

    // The branding without the image, for the banner preview
    public BrandingInfo Branding { get; private set; } = new(ServerSettings.DefaultServerName, null, false, 0);

    [TempData]
    public string? StatusMessage { get; set; }

    // If one connectivity check fails, the time between two updates of the last seen time is two check intervals
    public bool OneFailedCheckCausesTimeout =>
        Settings.AircraftConnectionTimeoutMinutes <= Settings.ConnectivityCheckIntervalMinutes * 2;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Settings = await _settingsProvider.Load(cancellationToken);
        Branding = await _settingsProvider.LoadBranding(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (Settings.VatsimStatusUrl is { IsAbsoluteUri: false })
            ModelState.AddModelError($"{nameof(Settings)}.{nameof(Settings.VatsimStatusUrl)}", "Enter an absolute URL.");

        if (!ModelState.IsValid)
        {
            Branding = await _settingsProvider.LoadBranding(cancellationToken);
            return Page();
        }

        await _mediator.Send(new SaveServerSettingsCommand(Settings), cancellationToken);

        StatusMessage = "The settings were saved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUploadBannerAsync(IFormFile? banner, CancellationToken cancellationToken)
    {
        // The settings fields are not in the banner form
        ModelState.Clear();

        if (banner is null || banner.Length == 0)
            ModelState.AddModelError("Banner", "Select an image.");
        else if (banner.Length > BannerImageFormat.MaxSize)
            ModelState.AddModelError("Banner", "The image is larger than 1 MB.");

        byte[]? image = null;
        if (ModelState.IsValid)
        {
            using var stream = new MemoryStream();
            await banner!.CopyToAsync(stream, cancellationToken);
            image = stream.ToArray();

            if (BannerImageFormat.FindContentType(image) is null)
                ModelState.AddModelError("Banner", "The image must be PNG, JPEG or WebP.");
        }

        if (!ModelState.IsValid)
        {
            await OnGetAsync(cancellationToken);
            return Page();
        }

        await _mediator.Send(new SetBannerImageCommand(image), cancellationToken);

        StatusMessage = "The banner was saved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveBannerAsync(CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetBannerImageCommand(null), cancellationToken);

        StatusMessage = "The banner was removed.";
        return RedirectToPage();
    }
}
