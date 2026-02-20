using CPDLCServer.Model;
using CPDLCServer.Pages.ViewModels;
using CPDLCServer.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Pages.Dashboard.Messages;

public class IndexModel : PageModel
{
    private readonly IDialogueRepository _dialogueRepository;
    private readonly IAircraftRepository _aircraftRepository;
    private const int PageSize = 50;

    public IndexModel(
        IDialogueRepository dialogueRepository,
        IAircraftRepository aircraftRepository)
    {
        _dialogueRepository = dialogueRepository;
        _aircraftRepository = aircraftRepository;
    }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public List<MessageRowViewModel> Messages { get; private set; } = [];
    public int TotalPages { get; private set; }
    public int CurrentPage => PageNumber;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var dialogues = await _dialogueRepository.All(cancellationToken);
        var aircraft = await _aircraftRepository.All(cancellationToken);
        var aircraftLookup = aircraft.ToDictionary(a => a.Callsign, a => a);

        var allMessages = dialogues
            .SelectMany(d => d.Messages.Select(m => ToViewModel(d, m, aircraftLookup)))
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        TotalPages = (int)Math.Ceiling(allMessages.Count / (double)PageSize);
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, TotalPages));

        Messages = allMessages
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }

    public static MessageRowViewModel ToViewModel(
        Model.Dialogue dialogue,
        ICpdlcMessage message,
        Dictionary<string, AircraftConnection> aircraftLookup)
    {
        var aircraft = aircraftLookup.GetValueOrDefault(dialogue.AircraftCallsign);
        var acarsNetwork = aircraft?.AcarsClientId ?? "Unknown";
        var stationId = aircraft?.StationId ?? "Unknown";

        return message switch
        {
            UplinkMessage uplink => new MessageRowViewModel(
                DialogueId: dialogue.Id,
                Timestamp: uplink.Sent,
                AcarsNetwork: acarsNetwork,
                IsUplink: true,
                Sender: $"{stationId} ({uplink.SenderCallsign})",
                Receiver: uplink.Recipient,
                AlertType: uplink.AlertType.ToString(),
                ResponseType: uplink.ResponseType.ToString(),
                Content: uplink.Content,
                IsClosed: uplink.IsClosed),

            DownlinkMessage downlink => new MessageRowViewModel(
                DialogueId: dialogue.Id,
                Timestamp: downlink.Received,
                AcarsNetwork: acarsNetwork,
                IsUplink: false,
                Sender: downlink.Sender,
                Receiver: stationId,
                AlertType: downlink.AlertType.ToString(),
                ResponseType: downlink.ResponseType.ToString(),
                Content: downlink.Content,
                IsClosed: downlink.IsClosed),

            _ => throw new InvalidOperationException("Unknown message type")
        };
    }
}
