using CPDLCServer.AdscContracts;
using CPDLCServer.AdscContracts.Reports;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Adsc;

public class IndexModel : PageModel
{
    private readonly IAdscContractRepository _adscContractRepository;
    private readonly IAdscReportRepository _adscReportRepository;

    public IndexModel(
        IAdscContractRepository adscContractRepository,
        IAdscReportRepository adscReportRepository)
    {
        _adscContractRepository = adscContractRepository;
        _adscReportRepository = adscReportRepository;
    }

    public AdscContract[] Contracts { get; private set; } = [];

    // The source of the latest report for each aircraft
    public Dictionary<string, AdscReportSource> ReportSources { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var contracts = await _adscContractRepository.All(cancellationToken);
        Contracts = contracts
            .OrderBy(c => c.Callsign)
            .ThenBy(c => c.StationId)
            .ToArray();

        var reports = await _adscReportRepository.AllLatest(cancellationToken);
        ReportSources = reports.ToDictionary(r => r.Callsign, r => r.Report.Source);
    }
}
