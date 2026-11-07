using CPDLCServer.Controllers;
using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Web.Pages.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages;

public class IndexModel : PageModel
{
    private const int PageSize = 50;

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

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public ControllerInfo[] Controllers { get; private set; } = [];
    public AircraftConnection[] Aircraft { get; private set; } = [];

    public int TotalMessageCount { get; private set; }
    public int OpenDialogueCount { get; private set; }

    public List<MessageRowViewModel> Messages { get; private set; } = [];
    public int TotalPages { get; private set; }
    public int CurrentPage => PageNumber;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Controllers = await _controllerRepository.All(cancellationToken);
        Aircraft = await _aircraftRepository.All(cancellationToken);
        var dialogues = await _dialogueRepository.All(cancellationToken);

        TotalMessageCount = dialogues.Sum(d => d.Messages.Count);
        OpenDialogueCount = dialogues.Count(d => !d.IsClosed);

        var aircraftLookup = Aircraft.ToDictionary(a => a.Callsign, a => a);
        var allMessages = dialogues
            .SelectMany(d => d.Messages.Select(m => MessageRowViewModel.From(d, m, aircraftLookup)))
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        TotalPages = (int)Math.Ceiling(allMessages.Count / (double)PageSize);
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, TotalPages));

        Messages = allMessages
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }
}
