using CPDLCServer.Model;
using CPDLCServer.Pages.ViewModels;
using CPDLCServer.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Pages.Dashboard.Pilot;

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

    public string Callsign { get; private set; } = string.Empty;
    public AircraftConnection? Aircraft { get; private set; }
    public List<MessageRowViewModel> Messages { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string callsign, CancellationToken cancellationToken)
    {
        Callsign = callsign;

        var dialogues = await _dialogueRepository.All(cancellationToken);
        var aircraft = await _aircraftRepository.All(cancellationToken);
        Aircraft = aircraft.FirstOrDefault(a => a.Callsign == callsign);
        var aircraftLookup = aircraft.ToDictionary(a => a.Callsign, a => a);

        Messages = dialogues
            .Where(d => d.AircraftCallsign == callsign)
            .SelectMany(d => d.Messages.Select(m => Dashboard.Messages.IndexModel.ToViewModel(d, m, aircraftLookup)))
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        return Page();
    }
}
