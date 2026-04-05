using CPDLCServer.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Pages.Dashboard;

public class IndexModel : PageModel
{
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

    public int AtcConnectionCount { get; private set; }
    public int PilotConnectionCount { get; private set; }
    public int TotalMessageCount { get; private set; }
    public int OpenDialogueCount { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var controllers = await _controllerRepository.All(cancellationToken);
        var aircraft = await _aircraftRepository.All(cancellationToken);
        var dialogues = await _dialogueRepository.All(cancellationToken);

        AtcConnectionCount = controllers.Length;
        PilotConnectionCount = aircraft.Length;
        TotalMessageCount = dialogues.Sum(d => d.Messages.Count);
        OpenDialogueCount = dialogues.Count(d => !d.IsClosed);
    }
}
