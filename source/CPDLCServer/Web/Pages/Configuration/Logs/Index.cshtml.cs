using CPDLCServer.AcarsConnections;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Configuration.Logs;

// The page gets the log entries from /logs/stream with JavaScript. The filters are in the query string, so that a
// link can open the page with a filter, for example the logs of one ACARS connection.
public class IndexModel : PageModel
{
    private readonly IAcarsConnectionManager _acarsConnectionManager;

    public IndexModel(IAcarsConnectionManager acarsConnectionManager)
    {
        _acarsConnectionManager = acarsConnectionManager;
    }

    public string[] AcarsConnectionIds { get; private set; } = [];

    public void OnGet()
    {
        AcarsConnectionIds = _acarsConnectionManager.GetAcarsConnectionIds().Order().ToArray();
    }
}
