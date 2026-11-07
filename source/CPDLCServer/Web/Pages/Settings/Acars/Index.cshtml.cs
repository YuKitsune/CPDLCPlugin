using CPDLCServer.AcarsConnections;
using CPDLCServer.AcarsConnections.ConnectionSettings;
using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Web.Pages.Settings.Acars;

public class IndexModel : PageModel
{
    private readonly CpdlcDbContext _dbContext;
    private readonly IAcarsConnectionManager _acarsConnectionManager;
    private readonly IUnitOfWorkMediator _mediator;
    private readonly ILogger _logger;

    public IndexModel(
        CpdlcDbContext dbContext,
        IAcarsConnectionManager acarsConnectionManager,
        IUnitOfWorkMediator mediator,
        ILogger logger)
    {
        _dbContext = dbContext;
        _acarsConnectionManager = acarsConnectionManager;
        _mediator = mediator;
        _logger = logger;
    }

    public AcarsConnectionSettings[] Connections { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public bool IsRunning(AcarsConnectionSettings settings) => _acarsConnectionManager.IsRunning(settings.Id);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Connections = await _dbContext.AcarsConnections
            .AsNoTracking()
            .OrderBy(c => c.StationIdentifier)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteAcarsConnectionCommand(id), cancellationToken);
            StatusMessage = "The ACARS connection was deleted.";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete ACARS connection {Id}", id);
            ErrorMessage = $"The ACARS connection was not deleted: {ex.Message}";
        }

        return RedirectToPage();
    }
}
