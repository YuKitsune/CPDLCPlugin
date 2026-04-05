using CPDLCServer.Model;
using CPDLCServer.Pages.ViewModels;
using CPDLCServer.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Pages.Dashboard.Atsu;

public class IndexModel : PageModel
{
    private readonly IDialogueRepository _dialogueRepository;
    private readonly IAircraftRepository _aircraftRepository;
    private readonly IControllerRepository _controllerRepository;

    public IndexModel(
        IDialogueRepository dialogueRepository,
        IAircraftRepository aircraftRepository,
        IControllerRepository controllerRepository)
    {
        _dialogueRepository = dialogueRepository;
        _aircraftRepository = aircraftRepository;
        _controllerRepository = controllerRepository;
    }

    public string StationId { get; private set; } = string.Empty;
    public ControllerInfo[] Controllers { get; private set; } = [];
    public List<MessageRowViewModel> Messages { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string stationId, CancellationToken cancellationToken)
    {
        StationId = stationId;

        var dialogues = await _dialogueRepository.All(cancellationToken);
        var aircraft = await _aircraftRepository.All(cancellationToken);
        var controllers = await _controllerRepository.All(cancellationToken);

        Controllers = controllers.Where(c => c.StationId == stationId).ToArray();

        var aircraftLookup = aircraft.ToDictionary(a => a.Callsign, a => a);
        var aircraftAtStation = aircraft
            .Where(a => a.StationId == stationId)
            .Select(a => a.Callsign)
            .ToHashSet();

        Messages = dialogues
            .Where(d => aircraftAtStation.Contains(d.AircraftCallsign))
            .SelectMany(d => d.Messages.Select(m => Dashboard.Messages.IndexModel.ToViewModel(d, m, aircraftLookup)))
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        return Page();
    }
}
