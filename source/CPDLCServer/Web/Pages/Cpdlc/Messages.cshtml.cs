using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Web.Pages.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages.Cpdlc;

public class MessagesModel : PageModel
{
    private const int PageSize = 50;

    private readonly IAircraftRepository _aircraftRepository;
    private readonly IDialogueRepository _dialogueRepository;

    public MessagesModel(IAircraftRepository aircraftRepository, IDialogueRepository dialogueRepository)
    {
        _aircraftRepository = aircraftRepository;
        _dialogueRepository = dialogueRepository;
    }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public List<MessageRowViewModel> Messages { get; private set; } = [];
    public PaginationViewModel Pagination { get; private set; } = new("/cpdlc/messages", 1, 1);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var aircraft = await _aircraftRepository.All(cancellationToken);
        var dialogues = await _dialogueRepository.All(cancellationToken);

        var aircraftLookup = aircraft.ToDictionary(a => a.Callsign, a => a);
        var allMessages = dialogues
            .SelectMany(d => d.Messages.Select(m => MessageRowViewModel.From(d, m, aircraftLookup)))
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        var totalPages = (int)Math.Ceiling(allMessages.Count / (double)PageSize);
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, totalPages));
        Pagination = new PaginationViewModel("/cpdlc/messages", PageNumber, totalPages);

        Messages = allMessages
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }
}
