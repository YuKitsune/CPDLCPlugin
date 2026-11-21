using CPDLCServer.AdscContracts.MessageLog;
using CPDLCServer.Web.Pages.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Adsc;

public class MessagesModel : PageModel
{
    private const int PageSize = 50;

    private readonly IAdscMessageLogRepository _adscMessageLogRepository;

    public MessagesModel(IAdscMessageLogRepository adscMessageLogRepository)
    {
        _adscMessageLogRepository = adscMessageLogRepository;
    }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public AdscMessageLogEntry[] LogEntries { get; private set; } = [];
    public PaginationViewModel Pagination { get; private set; } = new("/adsc/messages", 1, 1);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var logEntries = await _adscMessageLogRepository.All(cancellationToken);

        var totalPages = (int)Math.Ceiling(logEntries.Length / (double)PageSize);
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, totalPages));
        Pagination = new PaginationViewModel("/adsc/messages", PageNumber, totalPages);

        LogEntries = logEntries
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToArray();
    }
}
