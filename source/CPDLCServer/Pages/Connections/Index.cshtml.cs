using CPDLCServer.Model;
using CPDLCServer.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Pages.Dashboard.Connections;

public class IndexModel : PageModel
{
    private readonly IControllerRepository _controllerRepository;
    private readonly IAircraftRepository _aircraftRepository;

    public IndexModel(
        IControllerRepository controllerRepository,
        IAircraftRepository aircraftRepository)
    {
        _controllerRepository = controllerRepository;
        _aircraftRepository = aircraftRepository;
    }

    public ControllerInfo[] Controllers { get; private set; } = [];
    public AircraftConnection[] Aircraft { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Controllers = await _controllerRepository.All(cancellationToken);
        Aircraft = await _aircraftRepository.All(cancellationToken);
    }
}
