using CPDLCServer.AcarsConnections.Traffic;
using CPDLCServer.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Web.Pages.Configuration.Acars;

public class TrafficModel : PageModel
{
    private readonly CpdlcDbContext _dbContext;
    private readonly AcarsTrafficLog _trafficLog;

    public TrafficModel(CpdlcDbContext dbContext, AcarsTrafficLog trafficLog)
    {
        _dbContext = dbContext;
        _trafficLog = trafficLog;
    }

    [BindProperty(SupportsGet = true)]
    public bool ShowEmptyPolls { get; set; }

    public string AcarsConnectionId { get; private set; } = string.Empty;
    public AcarsTrafficEntry[] Entries { get; private set; } = [];

    public int Capacity => _trafficLog.Capacity;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await Load(id, cancellationToken))
            return NotFound();

        return Page();
    }

    // The rows only, for the automatic refresh
    public async Task<IActionResult> OnGetRowsAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await Load(id, cancellationToken))
            return NotFound();

        return Partial("_TrafficRows", Entries);
    }

    async Task<bool> Load(Guid id, CancellationToken cancellationToken)
    {
        var connection = await _dbContext.AcarsConnections
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (connection is null)
            return false;

        AcarsConnectionId = connection.AcarsConnectionId;
        Entries = _trafficLog.Get(AcarsConnectionId)
            .Where(e => ShowEmptyPolls || !e.IsEmptyPoll)
            .ToArray();
        return true;
    }
}
