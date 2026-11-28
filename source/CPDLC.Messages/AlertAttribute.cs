namespace CPDLC.Messages;

/// <summary>
/// The alert that the receiver of a message element gets.
/// </summary>
public enum AlertAttribute
{
    /// <summary>"H".</summary>
    High,

    /// <summary>"M". Most elements use this alert.</summary>
    Medium,

    /// <summary>"L".</summary>
    Low,

    /// <summary>"N".</summary>
    None
}
