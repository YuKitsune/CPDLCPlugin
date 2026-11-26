using CPDLCServer.Auth;
using CPDLCServer.Auth.PluginTokens;
using CPDLCServer.Infrastructure;
using CPDLCServer.Web.Pages.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Configuration.Tokens;

// The plugin tokens. The page never shows a token, because the server keeps only its hash.
public class IndexModel : PageModel
{
    private const int PageSize = 50;

    private readonly IPluginTokenRepository _pluginTokenRepository;
    private readonly IUnitOfWorkMediator _mediator;
    private readonly IClock _clock;

    public IndexModel(IPluginTokenRepository pluginTokenRepository, IUnitOfWorkMediator mediator, IClock clock)
    {
        _pluginTokenRepository = pluginTokenRepository;
        _mediator = mediator;
        _clock = clock;
    }

    // The start of the CID, for example 123 finds 1234567
    [BindProperty(SupportsGet = true)]
    public string? Cid { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    // The tokens to delete
    [BindProperty]
    public Guid[] SelectedIds { get; set; } = [];

    public PluginToken[] Tokens { get; private set; } = [];
    public int TotalCount { get; private set; }
    public DateTimeOffset Now { get; private set; }
    public PaginationViewModel Pagination { get; private set; } = new("/configuration/tokens", 1, 1);

    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsSearchValid => string.IsNullOrEmpty(Cid) || Cid.All(char.IsAsciiDigit);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Now = _clock.UtcNow();
        if (!IsSearchValid)
            return;

        var cidPrefix = string.IsNullOrEmpty(Cid) ? null : Cid;
        TotalCount = await _pluginTokenRepository.Count(cidPrefix, cancellationToken);

        var totalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, totalPages));
        Pagination = new PaginationViewModel(PathWithSearch(), PageNumber, totalPages);

        Tokens = await _pluginTokenRepository.Search(cidPrefix, (PageNumber - 1) * PageSize, PageSize, cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedIds.Length > 0)
        {
            var count = await _mediator.Send(new DeletePluginTokensCommand(SelectedIds, User.GetCid()!), cancellationToken);
            StatusMessage = count == 1 ? "1 token was deleted." : $"{count} tokens were deleted.";
        }

        return Redirect(PathWithSearch());
    }

    string PathWithSearch() =>
        string.IsNullOrEmpty(Cid) ? "/configuration/tokens" : $"/configuration/tokens?cid={Uri.EscapeDataString(Cid)}";
}
