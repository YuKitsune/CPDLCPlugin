using CPDLCServer.Controllers;
using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Web.Pages.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Cpdlc;

public class IndexModel : PageModel
{
    private const int RecentMessageCount = 10;

    private readonly IControllerRepository _controllerRepository;
    private readonly IAircraftRepository _aircraftRepository;
    private readonly IDialogueRepository _dialogueRepository;

    public IndexModel(
        IControllerRepository controllerRepository,
        IAircraftRepository aircraftRepository,
        IDialogueRepository dialogueRepository)
    {
        _controllerRepository = controllerRepository;
        _aircraftRepository = aircraftRepository;
        _dialogueRepository = dialogueRepository;
    }

    public ControllerInfo[] Controllers { get; private set; } = [];
    public AircraftConnection[] Aircraft { get; private set; } = [];

    public int TotalMessageCount { get; private set; }
    public int OpenDialogueCount { get; private set; }

    public List<MessageRowViewModel> RecentMessages { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Controllers = (await _controllerRepository.All(cancellationToken))
            .OrderBy(c => c.StationId)
            .ThenBy(c => c.Callsign)
            .ToArray();
        Aircraft = (await _aircraftRepository.All(cancellationToken))
            .OrderBy(a => a.Callsign)
            .ToArray();
        var dialogues = await _dialogueRepository.All(cancellationToken);

        TotalMessageCount = dialogues.Sum(d => d.Messages.Count);
        OpenDialogueCount = dialogues.Count(d => !d.IsClosed);

        var aircraftLookup = Aircraft.ToDictionary(a => a.Callsign, a => a);
        RecentMessages = dialogues
            .SelectMany(d => d.Messages.Select(m => MessageRowViewModel.From(d, m, aircraftLookup)))
            .OrderByDescending(m => m.Timestamp)
            .Take(RecentMessageCount)
            .ToList();
    }
}
