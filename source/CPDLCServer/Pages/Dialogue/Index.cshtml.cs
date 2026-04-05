using CPDLCServer.Model;
using CPDLCServer.Pages.ViewModels;
using CPDLCServer.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Pages.Dashboard.Dialogue;

public class IndexModel : PageModel
{
    private readonly IDialogueRepository _dialogueRepository;
    private readonly IAircraftRepository _aircraftRepository;

    public IndexModel(
        IDialogueRepository dialogueRepository,
        IAircraftRepository aircraftRepository)
    {
        _dialogueRepository = dialogueRepository;
        _aircraftRepository = aircraftRepository;
    }

    public Model.Dialogue? Dialogue { get; private set; }
    public List<MessageRowViewModel> Messages { get; private set; } = [];
    public string AcarsNetwork { get; private set; } = "Unknown";
    public string StationId { get; private set; } = "Unknown";

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Dialogue = await _dialogueRepository.FindById(id, cancellationToken);

        if (Dialogue is null)
        {
            return NotFound();
        }

        var aircraft = await _aircraftRepository.All(cancellationToken);
        var match = aircraft.FirstOrDefault(a => a.Callsign == Dialogue.AircraftCallsign);
        AcarsNetwork = match?.AcarsClientId ?? "Unknown";
        StationId = match?.StationId ?? "Unknown";

        var aircraftLookup = aircraft.ToDictionary(a => a.Callsign, a => a);

        Messages = Dialogue.Messages
            .OrderBy(m => m.Time)
            .Select(m => Dashboard.Messages.IndexModel.ToViewModel(Dialogue, m, aircraftLookup))
            .ToList();

        return Page();
    }
}
