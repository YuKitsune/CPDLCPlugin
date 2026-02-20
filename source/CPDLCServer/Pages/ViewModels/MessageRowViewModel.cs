namespace CPDLCServer.Pages.ViewModels;

public record MessageRowViewModel(
    Guid DialogueId,
    DateTimeOffset Timestamp,
    string AcarsNetwork,
    bool IsUplink,
    string Sender,
    string Receiver,
    string AlertType,
    string ResponseType,
    string Content,
    bool IsClosed);
