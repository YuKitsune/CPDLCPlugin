using CPDLCServer.AcarsConnections.ConnectionSettings;
using CPDLCServer.Infrastructure;
using CPDLCServer.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Web.Pages.Settings.Acars;

public class EditModel : PageModel
{
    private readonly CpdlcDbContext _dbContext;
    private readonly IUnitOfWorkMediator _mediator;
    private readonly ILogger _logger;

    public EditModel(
        CpdlcDbContext dbContext,
        IUnitOfWorkMediator mediator,
        ILogger logger)
    {
        _dbContext = dbContext;
        _mediator = mediator;
        _logger = logger;
    }

    [BindProperty]
    public AcarsConnectionSettings Connection { get; set; } = new();

    // Plain text. It is not shown. For an existing connection, an empty value keeps the current code.
    [BindProperty]
    public string? AuthenticationCode { get; set; }

    public AcarsConnectionSettings Defaults { get; } = new();

    // Null for a new connection
    public Guid? Id { get; private set; }

    public bool IsNew => Id is null;

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken cancellationToken)
    {
        Id = id;
        if (id is null)
            return Page();

        var connection = await _dbContext.AcarsConnections
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id.Value, cancellationToken);
        if (connection is null)
            return NotFound();

        Connection = connection;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id, CancellationToken cancellationToken)
    {
        Id = id;

        if (id is null && string.IsNullOrWhiteSpace(AuthenticationCode))
            ModelState.AddModelError(nameof(AuthenticationCode), "Enter the authentication code.");

        if (Connection.Url is { IsAbsoluteUri: false })
            ModelState.AddModelError($"{nameof(Connection)}.{nameof(Connection.Url)}", "Enter an absolute URL.");

        // The ACARS connection ID comes from the station identifier, so only one connection can use a station identifier
        var stationIdentifier = Connection.StationIdentifier.ToUpperInvariant();
        var otherId = id ?? Guid.Empty;
        if (await _dbContext.AcarsConnections.AnyAsync(c => c.Id != otherId && c.StationIdentifier == stationIdentifier, cancellationToken))
            ModelState.AddModelError($"{nameof(Connection)}.{nameof(Connection.StationIdentifier)}", "Another ACARS connection uses this station identifier.");

        if (!ModelState.IsValid)
            return Page();

        try
        {
            if (id is null)
            {
                await _mediator.Send(new CreateAcarsConnectionCommand(Connection, AuthenticationCode), cancellationToken);
            }
            else
            {
                await _mediator.Send(new UpdateAcarsConnectionCommand(id.Value, Connection, AuthenticationCode), cancellationToken);
            }
        }
        catch (AggregateException ex)
        {
            // The unit of work saved the settings, but the connection did not start
            _logger.Error(ex, "ACARS connection for {StationId} was saved, but did not start", stationIdentifier);
            ErrorMessage = $"The ACARS connection for {stationIdentifier} was saved, but it did not start: {ex.InnerException?.Message}";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save ACARS connection for {StationId}", stationIdentifier);
            ModelState.AddModelError(string.Empty, $"The ACARS connection was not saved: {ex.Message}");
            return Page();
        }

        StatusMessage = $"The ACARS connection for {stationIdentifier} was saved.";
        return RedirectToPage("Index");
    }
}
