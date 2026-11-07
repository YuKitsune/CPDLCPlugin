namespace CPDLCServer.Cpdlc.Dialogues;

public abstract class CpdlcMessage(
    Guid dialogueId,
    int messageId,
    int? messageReference,
    AlertType alertType)
{
    public Guid DialogueId { get; } = dialogueId;
    public int MessageId { get; } = messageId;
    public int? MessageReference { get; } = messageReference;
    public AlertType AlertType { get; } = alertType;

    public abstract DateTimeOffset Time { get; }

    // The place of the message in its dialogue. The dialogue sets it.
    public int Position { get; internal set; }

    public DateTimeOffset? Closed { get; protected set; }
    public bool IsClosed => Closed is not null;

    public DateTimeOffset? Acknowledged { get; protected set; }
    public bool IsAcknowledged => Acknowledged is not null;

    public abstract void Close(DateTimeOffset time);
}
