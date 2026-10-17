using CPDLCServer.Model;
using CPDLCServer.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Pages.Dashboard.Adsc;

public class IndexModel : PageModel
{
    private readonly IAdscContractRepository _adscContractRepository;
    private readonly IAdscMessageLogRepository _adscMessageLogRepository;
    private readonly IAdscReportRepository _adscReportRepository;
    private const int PageSize = 50;

    public IndexModel(
        IAdscContractRepository adscContractRepository,
        IAdscMessageLogRepository adscMessageLogRepository,
        IAdscReportRepository adscReportRepository)
    {
        _adscContractRepository = adscContractRepository;
        _adscMessageLogRepository = adscMessageLogRepository;
        _adscReportRepository = adscReportRepository;
    }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public AdscContract[] Contracts { get; private set; } = [];
    // The source of the latest report for each aircraft
    public Dictionary<string, AdscReportSource> ReportSources { get; private set; } = new();
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

        var reports = await _adscReportRepository.All(cancellationToken);
        ReportSources = reports.ToDictionary(r => r.Callsign, r => r.Report.Source);

        var logEntries = await _adscMessageLogRepository.All(cancellationToken);

        TotalPages = (int)Math.Ceiling(logEntries.Length / (double)PageSize);
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, TotalPages));

        LogEntries = logEntries
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToArray();
    }
}
