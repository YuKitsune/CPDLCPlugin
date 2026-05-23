namespace CPDLCServer.Model;

public interface ICpdlcMessage
{
    public Guid DialogueId { get; }
    public int MessageId { get; }
    public int? MessageReference { get; }
    AlertType AlertType { get; }
    DateTimeOffset Time { get; }
    
    DateTimeOffset? Closed { get; }
    bool IsClosed { get; }
    
    DateTimeOffset? Acknowledged { get; }
    bool IsAcknowledged { get; }
    
    void Close(DateTimeOffset time);
}