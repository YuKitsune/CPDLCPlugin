using CPDLC.Messages;

namespace CPDLCServer.Cpdlc.Dialogues;

public enum AlertType
{
    High,
    Medium,
    Low,
    None
}

public static class AlertTypeMapping
{
    public static AlertType From(AlertAttribute alertAttribute)
    {
        return alertAttribute switch
        {
            AlertAttribute.High => AlertType.High,
            AlertAttribute.Medium => AlertType.Medium,
            AlertAttribute.Low => AlertType.Low,
            AlertAttribute.None => AlertType.None,
            _ => throw new ArgumentOutOfRangeException(nameof(alertAttribute), alertAttribute, null)
        };
    }
}
