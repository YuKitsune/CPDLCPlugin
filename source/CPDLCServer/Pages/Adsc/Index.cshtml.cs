using CPDLCServer.Model;
using CPDLCServer.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Pages.Dashboard.Adsc;

public class IndexModel : PageModel
{
    private readonly IAdscContractRepository _adscContractRepository;
    private readonly IAdscMessageLogRepository _adscMessageLogRepository;
    private const int PageSize = 50;

    public IndexModel(
        IAdscContractRepository adscContractRepository,
        IAdscMessageLogRepository adscMessageLogRepository)
    {
        _adscContractRepository = adscContractRepository;
        _adscMessageLogRepository = adscMessageLogRepository;
    }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public AdscContract[] Contracts { get; private set; } = [];
    public AdscMessageLogEntry[] LogEntries { get; private set; } = [];
    public int TotalPages { get; private set; }
    public int CurrentPage => PageNumber;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var contracts = await _adscContractRepository.All(cancellationToken);
        Contracts = contracts
            .OrderBy(c => c.Callsign)
            .ThenBy(c => c.StationId)
            .ToArray();

        var logEntries = await _adscMessageLogRepository.All(cancellationToken);

        TotalPages = (int)Math.Ceiling(logEntries.Length / (double)PageSize);
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, TotalPages));

        LogEntries = logEntries
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToArray();
    }
}
